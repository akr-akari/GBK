#define D_AKR_TEST
#include "akr_test.hh"

#include "../gbk.hh"

#include <cstdio>

int main()
{
    // "Hello, 世界!";
    const char* gbkString = "Hello, \xCA\xC0\xBD\xE7!";

    char*       gbkOutput;
    std::size_t gbkLength;

    char16_t* u16String = nullptr;
    auto u16Length = akr::GBK::Decode(gbkString, gbkString + 12, u16String);
    u16String = new char16_t[u16Length + 1] {};
    akr::GBK::Decode(gbkString, gbkString + 12, u16String);

    gbkOutput = nullptr;
    gbkLength = akr::GBK::Encode(u16String, u16String + 10, gbkOutput);
    gbkOutput = new char[gbkLength + 1] {};
    akr::GBK::Encode(u16String, u16String + 10, gbkOutput);

    for (auto p = u16String; *p; p++)
    {
        std::printf("%X ", static_cast<unsigned int>(*p));
    }
    std::puts("");

    std::puts(gbkOutput);

    delete[] u16String;
    delete[] gbkOutput;

    char32_t* u32String = nullptr;
    auto u32Length = akr::GBK::Decode(gbkString, gbkString + 12, u32String);
    u32String = new char32_t[u32Length + 1] {};
    akr::GBK::Decode(gbkString, gbkString + 12, u32String);

    gbkOutput = nullptr;
    gbkLength = akr::GBK::Encode(u32String, u32String + 10, gbkOutput);
    gbkOutput = new char[gbkLength + 1] {};
    akr::GBK::Encode(u32String, u32String + 10, gbkOutput);

    for (auto p = u32String; *p; p++)
    {
        std::printf("%X ", static_cast<unsigned int>(*p));
    }
    std::puts("");

    std::puts(gbkOutput);

    delete[] u32String;
    delete[] gbkOutput;
}
