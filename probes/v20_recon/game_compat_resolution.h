#pragma once
// Read-only compatibility primitives. No game, Windows, JSON, writes or calls.
// These helpers run during preparation, never inside an animation callback.
#include <algorithm>
#include <cstddef>
#include <cstdint>
#include <limits>
#include <string>
#include <utility>
#include <vector>

namespace ww_game_compat_resolution {
using Address=uint64_t;
inline constexpr size_t kMaximumPatternBytes=1024*1024;
struct Range {Address address=0,bytes=0;};
inline bool ValidRange(const Range& r){return r.bytes&&r.address<=(std::numeric_limits<Address>::max)()-r.bytes;}
inline bool Contains(const Range& r,Address address,Address bytes){return ValidRange(r)&&bytes&&address>=r.address&&address-r.address<r.bytes&&bytes<=r.bytes-(address-r.address);}
inline bool ResolveImageAddress(Address image,Address imageBytes,Address rva,Address bytes,Address& address){
    address=0;if(!ValidRange({image,imageBytes})||!bytes||rva>=imageBytes||bytes>imageBytes-rva)return false;address=image+rva;return true;
}
inline int HexDigit(char c){return c>='0'&&c<='9'?c-'0':c>='a'&&c<='f'?c-'a'+10:c>='A'&&c<='F'?c-'A'+10:-1;}
inline bool ParseHexU64(const std::string& text,Address& value,std::string& error){
    value=0;error.clear();size_t start=0;
    if(text.size()>=2&&text[0]=='0'&&(text[1]=='x'||text[1]=='X'))start=2;
    if(start==text.size()){error="empty hexadecimal integer";return false;}
    Address parsed=0;for(size_t i=start;i<text.size();++i){const int digit=HexDigit(text[i]);
        if(digit<0){error="invalid hexadecimal integer";return false;}
        if(parsed>((std::numeric_limits<Address>::max)()-Address(digit))/16){error="hexadecimal integer overflow";return false;}
        parsed=parsed*16+Address(digit);}
    value=parsed;return true;
}
inline bool ParseHexBytes(const std::string& text,std::vector<uint8_t>& bytes,std::string& error){
    bytes.clear();error.clear();
    if(text.empty()||(text.size()&1)||text.size()/2>kMaximumPatternBytes){error="hex bytes must contain complete nonempty byte pairs within the size limit";return false;}
    std::vector<uint8_t> parsed;parsed.reserve(text.size()/2);
    for(size_t i=0;i<text.size();i+=2){const int hi=HexDigit(text[i]),lo=HexDigit(text[i+1]);
        if(hi<0||lo<0){error="invalid hexadecimal byte";return false;}parsed.push_back(uint8_t(hi*16+lo));}
    bytes=std::move(parsed);return true;
}
struct Pattern {std::vector<uint8_t> bytes,mask;};
inline bool ValidPattern(const Pattern& p){return !p.bytes.empty()&&p.bytes.size()<=kMaximumPatternBytes&&p.bytes.size()==p.mask.size()&&std::any_of(p.mask.begin(),p.mask.end(),[](uint8_t m){return m!=0;});}
inline bool ParsePattern(const std::string& bytesHex,const std::string& maskHex,Pattern& pattern,std::string& error){
    pattern={};Pattern parsed;if(!ParseHexBytes(bytesHex,parsed.bytes,error))return false;
    if(maskHex.empty())parsed.mask.assign(parsed.bytes.size(),0xff);
    else if(!ParseHexBytes(maskHex,parsed.mask,error))return false;
    if(!ValidPattern(parsed)){error="signature mask must match the byte length and contain constrained bits";return false;}
    pattern=std::move(parsed);return true;
}
inline bool Matches(const uint8_t* bytes,const Pattern& pattern){
    for(size_t i=0;i<pattern.bytes.size();++i)if(((bytes[i]^pattern.bytes[i])&pattern.mask[i])!=0)return false;return true;
}
enum class VerifyStatus {Match,Mismatch,InvalidRange,InvalidBytes,ReadFailed};
inline const char* Name(VerifyStatus status){switch(status){case VerifyStatus::Match:return "match";case VerifyStatus::Mismatch:return "mismatch";case VerifyStatus::InvalidRange:return "invalid_range";case VerifyStatus::InvalidBytes:return "invalid_bytes";default:return "read_failed";}}
template<class Read> VerifyStatus VerifyBytes(Read&& read,Address image,Address imageBytes,Address rva,const std::vector<uint8_t>& expected){
    if(expected.empty()||expected.size()>kMaximumPatternBytes)return VerifyStatus::InvalidBytes;
    Address address=0;if(!ResolveImageAddress(image,imageBytes,rva,expected.size(),address))return VerifyStatus::InvalidRange;
    std::vector<uint8_t> actual(expected.size());if(!read(address,actual.data(),actual.size()))return VerifyStatus::ReadFailed;
    return actual==expected?VerifyStatus::Match:VerifyStatus::Mismatch;
}
enum class ScanStatus {Unique,NotFound,Ambiguous,InvalidRange,InvalidPattern,ReadFailed};
inline const char* Name(ScanStatus status){switch(status){case ScanStatus::Unique:return "unique_candidate";case ScanStatus::NotFound:return "not_found";case ScanStatus::Ambiguous:return "ambiguous";case ScanStatus::InvalidRange:return "invalid_range";case ScanStatus::InvalidPattern:return "invalid_pattern";default:return "read_failed";}}
struct ScanResult {ScanStatus status=ScanStatus::NotFound;Address address=0;unsigned matches=0;bool complete=false;};
template<class Read> ScanResult FindUnique(Read&& read,const std::vector<Range>& input,const Pattern& pattern,size_t chunkBytes=64*1024){
    ScanResult result;
    if(!ValidPattern(pattern)||!chunkBytes||chunkBytes>4*1024*1024){result.status=ScanStatus::InvalidPattern;return result;}
    auto ranges=input;std::sort(ranges.begin(),ranges.end(),[](const Range& a,const Range& b){return a.address<b.address;});
    for(size_t i=0;i<ranges.size();++i)if(!ValidRange(ranges[i])||(i&&ranges[i].address<ranges[i-1].address+ranges[i-1].bytes)){
        result.status=ScanStatus::InvalidRange;return result;}
    std::vector<uint8_t> block(chunkBytes+pattern.bytes.size()-1);Address candidate=0;
    for(const auto& range:ranges){if(range.bytes<pattern.bytes.size())continue;
        const Address starts=range.bytes-pattern.bytes.size()+1;
        for(Address offset=0;offset<starts;){const size_t count=size_t((std::min)(Address(chunkBytes),starts-offset));
            const size_t readBytes=count+pattern.bytes.size()-1;
            if(!Contains(range,range.address+offset,readBytes)||!read(range.address+offset,block.data(),readBytes)){
                result.status=ScanStatus::ReadFailed;result.address=0;return result;}
            for(size_t i=0;i<count;++i)if(Matches(block.data()+i,pattern)){
                ++result.matches;if(result.matches>1){result.status=ScanStatus::Ambiguous;return result;}candidate=range.address+offset+i;}
            offset+=count;
        }
    }
    result.complete=true;result.address=candidate;result.status=result.matches==1?ScanStatus::Unique:ScanStatus::NotFound;return result;
}
inline uint16_t U16(const uint8_t* p){return uint16_t(p[0])|uint16_t(uint16_t(p[1])<<8);}
inline uint32_t U32(const uint8_t* p){return uint32_t(p[0])|(uint32_t(p[1])<<8)|(uint32_t(p[2])<<16)|(uint32_t(p[3])<<24);}
// Parse a loaded PE32+ image through the reader; never interpret unchecked
// pointers. Only mapped executable section spans become scan candidates.
template<class Read> bool ExecutableRanges(Read&& read,Address image,Address imageBytes,std::vector<Range>& output,std::string& error){
    output.clear();error.clear();const Range bounds{image,imageBytes};
    auto get=[&](Address rva,void* out,size_t bytes){return ValidRange(bounds)&&rva<imageBytes&&bytes<=imageBytes-rva&&read(image+rva,out,bytes);};
    uint8_t dos[64]{};if(!get(0,dos,sizeof(dos))||U16(dos)!=0x5a4d){error="DOS header unavailable";return false;}
    const Address nt=U32(dos+60);uint8_t head[24]{};
    if(nt<sizeof(dos)||!get(nt,head,sizeof(head))||U32(head)!=0x00004550||U16(head+4)!=0x8664){error="AMD64 PE header unavailable";return false;}
    const unsigned sections=U16(head+6),optionalBytes=U16(head+20);
    if(!sections||sections>96||optionalBytes<112){error="unsupported PE header bounds";return false;}
    if(nt>imageBytes||24>imageBytes-nt||optionalBytes>imageBytes-nt-24){error="optional header out of bounds";return false;}
    std::vector<uint8_t> optional(optionalBytes);if(!get(nt+24,optional.data(),optional.size())||U16(optional.data())!=0x20b){error="PE32+ optional header unavailable";return false;}
    if(U32(optional.data()+56)!=imageBytes){error="mapped image extent differs from PE SizeOfImage";return false;}
    const Address table=nt+24+optionalBytes;
    if(table>imageBytes||Address(sections)*40>imageBytes-table){error="section table out of bounds";return false;}
    std::vector<Range> ranges;
    for(unsigned i=0;i<sections;++i){uint8_t section[40]{};
        if(!get(table+Address(i)*40,section,sizeof(section))){error="section header unreadable";return false;}
        if(!(U32(section+36)&0x20000000u))continue;
        const Address size=(std::max)(U32(section+8),U32(section+16)),rva=U32(section+12);
        if(!size||rva>=imageBytes||size>imageBytes-rva){error="executable section out of bounds";return false;}
        ranges.push_back({image+rva,size});
    }
    if(ranges.empty()){error="no executable sections";return false;}
    std::sort(ranges.begin(),ranges.end(),[](const Range& a,const Range& b){return a.address<b.address;});
    for(size_t i=1;i<ranges.size();++i)if(ranges[i].address<ranges[i-1].address+ranges[i-1].bytes){error="overlapping executable sections";return false;}
    output=std::move(ranges);return true;
}
} // namespace ww_game_compat_resolution
