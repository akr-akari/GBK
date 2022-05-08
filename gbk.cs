using System;
using System.IO;
using System.Text;

class GBK
{
    static void GenerateDecodeTable(StreamWriter gbkHeaderWriter)
    {
        gbkHeaderWriter.WriteLine(@"        static constexpr std::uint16_t DecodeTable[] =
        {");

        var count = 0;

        for (byte b1 = 0x81; b1 <= 0xFE; b1++)
        {
            for (byte b2 = 0x40; b2 <= 0xFE; b2++)
            {
                if (b2 == 0x7F)
                {
                    continue;
                }

                var chars = Encoding.GetEncoding("GBK").GetChars(new byte[] { b1, b2 });

                System.Diagnostics.Debug.Assert(chars.Length == 1);

                var space1 = (count % 12 != 0) ? "" : "            ";

                count++;

                var space2 = (count % 12 != 0) ? " " : "\n";

                gbkHeaderWriter.Write($"{space1}0x{(UInt16)chars[0]:X4},{space2}");
            }
        }

        gbkHeaderWriter.WriteLine(@"        };
");
    }

    static void GenerateEncodeTable(StreamWriter gbkHeaderWriter)
    {
        gbkHeaderWriter.WriteLine(@"        static constexpr const char*   EncodeTable[] =
        {");

        var count = 0;

        for (char c = '\u00A4'; c <= '\uFFE5'; c++)
        {
            var bytes = Encoding.GetEncoding("GBK").GetBytes(c.ToString());

            System.Diagnostics.Debug.Assert(bytes.Length == 1 || bytes.Length == 2);

            var space1 = (count % 8 != 0) ? "" : "            ";

            count++;

            var space2 = (count % 8 != 0) ? " " : "\n";

            gbkHeaderWriter.Write($"{space1}\"\\x{bytes[0]:X2}\\x{bytes[bytes.Length - 1]:X2}\",{space2}");
        }

        gbkHeaderWriter.WriteLine(@"
        };
");
    }

    static void Main()
    {
        var gbkHeaderName = "gbk.hh";

        using (var gbkHeaderWriter = new StreamWriter(gbkHeaderName, false, new UTF8Encoding(false)))
        {
            gbkHeaderWriter.NewLine = "\n";

            gbkHeaderWriter.WriteLine(@"#ifndef Z_AKR_GBK_HH
#define Z_AKR_GBK_HH

#include <bit>
#include <cstddef>
#include <cstdint>

namespace akr
{
    struct GBK final
    {
        private:
        using Byte = unsigned char;

        private:");

            GenerateDecodeTable(gbkHeaderWriter);

            GenerateEncodeTable(gbkHeaderWriter);

            gbkHeaderWriter.WriteLine(@"        private:
        template<class Char>
        static constexpr auto decode(const Byte* srcBegin, const Byte* srcEnd, Char* dstBegin) noexcept -> std::size_t
        {
            auto dstLength = std::size_t();

            for (; srcBegin < srcEnd; srcBegin++, dstLength++)
            {
                if (*srcBegin <= 0x7F)
                {
                    if (dstBegin)
                    {
                        dstBegin[dstLength] = static_cast<Char>(*srcBegin);
                    }
                }
                else if ((0x81 <= srcBegin[0] && srcBegin[0] <= 0xFE) || (0x40 <= srcBegin[1] && srcBegin[1] <= 0xFE))
                {
                    if (dstBegin)
                    {
                        auto offset = srcBegin[1] > 0x7F;

                        dstBegin[dstLength] = DecodeTable[(srcBegin[0] - 0x81) * 190 + (srcBegin[1] - 0x40 - offset)];
                    }
                    srcBegin++;
                }
                else
                {
                    if (dstBegin)
                    {
                        dstBegin[dstLength] = 0x003F;
                    }
                }
            }

            return dstLength;
        }

        template<class Char>
        static constexpr auto encode(const Char* srcBegin, const Char* srcEnd, char* dstBegin) noexcept -> std::size_t
        {
            auto dstLength = std::size_t();

            for (; srcBegin < srcEnd; srcBegin++, dstLength++)
            {
                if (*srcBegin <= 0x007F)
                {
                    if (dstBegin)
                    {
                        dstBegin[dstLength] = static_cast<char>(*srcBegin);
                    }
                }
                else if (0x00A4 <= *srcBegin && *srcBegin <= 0xFFE5)
                {
                    if (dstBegin)
                    {
                        auto gbkChars = EncodeTable[*srcBegin - 0x00A4];

                        dstBegin[dstLength + 0] = gbkChars[0];
                        dstBegin[dstLength + 1] = gbkChars[1];
                    }
                    dstLength++;
                }
                else
                {
                    if (dstBegin)
                    {
                        dstBegin[dstLength + 0] = 0x3F;
                        dstBegin[dstLength + 1] = 0x3F;
                    }
                    dstLength++;
                }
            }

            return dstLength;
        }

        public:
        static constexpr auto Decode(const char*     srcBegin, const char*     srcEnd, char16_t* dstBegin)
            noexcept -> std::size_t
        {
            return decode(std::bit_cast<const Byte*>(srcBegin), std::bit_cast<const Byte*>(srcEnd), dstBegin);
        }

        static constexpr auto Encode(const char16_t* srcBegin, const char16_t* srcEnd, char*     dstBegin)
            noexcept -> std::size_t
        {
            return encode(srcBegin, srcEnd, dstBegin);
        }

        static constexpr auto Decode(const char*     srcBegin, const char*     srcEnd, char32_t* dstBegin)
            noexcept -> std::size_t
        {
            return decode(std::bit_cast<const Byte*>(srcBegin), std::bit_cast<const Byte*>(srcEnd), dstBegin);
        }

        static constexpr auto Encode(const char32_t* srcBegin, const char32_t* srcEnd, char*     dstBegin)
            noexcept -> std::size_t
        {
            return encode(srcBegin, srcEnd, dstBegin);
        }
    };
}

#ifdef  D_AKR_TEST
#include <cstring>

namespace akr::test
{
    AKR_TEST(GBK,
    {
        auto test = [](const char*     gbkString, std::size_t gbkLength,
                       const char16_t* u16String, std::size_t u16Length,
                       const char32_t* u32String, std::size_t u32Length)
        {
            {
                char16_t* u16String1 = nullptr;
                auto u16Length1 = GBK::Decode(gbkString, gbkString + gbkLength, u16String1);
                assert(u16Length1 == u16Length);

                u16String1 = new char16_t[u16Length1 + 1] {};
                u16Length1 = GBK::Decode(gbkString, gbkString + gbkLength, u16String1);
                assert(u16Length1 == u16Length);
                assert(!std::memcmp(u16String1, u16String, sizeof(char16_t) * u16Length1));

                char*     gbkString1 = nullptr;
                auto gbkLength1 = GBK::Encode(u16String1, u16String1 + u16Length1, gbkString1);
                assert(gbkLength1 == gbkLength);

                gbkString1 = new char    [gbkLength1 + 1] {};
                gbkLength1 = GBK::Encode(u16String1, u16String1 + u16Length1, gbkString1);
                assert(gbkLength1 == gbkLength);
                assert(!std::memcmp(gbkString1, gbkString, sizeof(char    ) * gbkLength1));

                delete[] u16String1;
                delete[] gbkString1;
            }
            {
                char32_t* u32String1 = nullptr;
                auto u32Length1 = GBK::Decode(gbkString, gbkString + gbkLength, u32String1);
                assert(u32Length1 == u32Length);

                u32String1 = new char32_t[u32Length1 + 1] {};
                u32Length1 = GBK::Decode(gbkString, gbkString + gbkLength, u32String1);
                assert(u32Length1 == u32Length);
                assert(!std::memcmp(u32String1, u32String, sizeof(char32_t) * u32Length1));

                char*     gbkString1 = nullptr;
                auto gbkLength1 = GBK::Encode(u32String1, u32String1 + u32Length1, gbkString1);
                assert(gbkLength1 == gbkLength);

                gbkString1 = new char    [gbkLength1 + 1] {};
                gbkLength1 = GBK::Encode(u32String1, u32String1 + u32Length1, gbkString1);
                assert(gbkLength1 == gbkLength);
                assert(!std::memcmp(gbkString1, gbkString, sizeof(char    ) * gbkLength1));

                delete[] u32String1;
                delete[] gbkString1;
            }
        };

        // Hello, 世界!
        test(""Hello, \xCA\xC0\xBD\xE7!"", 12,
            u""Hello, \u4E16\u754C!"", 10,
            U""Hello, \u4E16\u754C!"", 10);
    });
}
#endif//D_AKR_TEST

#endif//Z_AKR_GBK_HH");
        }
    }
}
