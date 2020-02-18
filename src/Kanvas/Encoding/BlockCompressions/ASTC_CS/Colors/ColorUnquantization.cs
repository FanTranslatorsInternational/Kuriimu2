using Kanvas.Encoding.BlockCompressions.ASTC_CS.Models;
using Kanvas.Encoding.BlockCompressions.ASTC_CS.Types;

namespace Kanvas.Encoding.BlockCompressions.ASTC_CS.Colors
{
    static class ColorUnquantization
    {
        private static readonly byte[][] ColorUnquantizationTable = {
            new byte[] {
                0, 255
            },
            new byte[] {
                0, 128, 255
            },
            new byte[] {
                0, 85, 170, 255
            },
            new byte[] {
                0, 64, 128, 192, 255
            },
            new byte[] {
                0, 255, 51, 204, 102, 153
            },
            new byte[] {
                0, 36, 73, 109, 146, 182, 219, 255
            },
            new byte[] {
                0, 255, 28, 227, 56, 199, 84, 171, 113, 142
            },
            new byte[] {
                0, 255, 69, 186, 23, 232, 92, 163, 46, 209, 116, 139
            },
            new byte[] {
                0, 17, 34, 51, 68, 85, 102, 119, 136, 153, 170, 187, 204, 221, 238, 255
            },
            new byte[] {
                0, 255, 67, 188, 13, 242, 80, 175, 27, 228, 94, 161, 40, 215, 107, 148,
                54, 201, 121, 134
            },
            new byte[] {
                0, 255, 33, 222, 66, 189, 99, 156, 11, 244, 44, 211, 77, 178, 110, 145,
                22, 233, 55, 200, 88, 167, 121, 134
            },
            new byte[] {
                0, 8, 16, 24, 33, 41, 49, 57, 66, 74, 82, 90, 99, 107, 115, 123,
                132, 140, 148, 156, 165, 173, 181, 189, 198, 206, 214, 222, 231, 239, 247, 255
            },
            new byte[] {
                0, 255, 32, 223, 65, 190, 97, 158, 6, 249, 39, 216, 71, 184, 104, 151,
                13, 242, 45, 210, 78, 177, 110, 145, 19, 236, 52, 203, 84, 171, 117, 138,
                26, 229, 58, 197, 91, 164, 123, 132
            },
            new byte[] {
                0, 255, 16, 239, 32, 223, 48, 207, 65, 190, 81, 174, 97, 158, 113, 142,
                5, 250, 21, 234, 38, 217, 54, 201, 70, 185, 86, 169, 103, 152, 119, 136,
                11, 244, 27, 228, 43, 212, 59, 196, 76, 179, 92, 163, 108, 147, 124, 131
            },
            new byte[] {
                0, 4, 8, 12, 16, 20, 24, 28, 32, 36, 40, 44, 48, 52, 56, 60,
                65, 69, 73, 77, 81, 85, 89, 93, 97, 101, 105, 109, 113, 117, 121, 125,
                130, 134, 138, 142, 146, 150, 154, 158, 162, 166, 170, 174, 178, 182, 186, 190,
                195, 199, 203, 207, 211, 215, 219, 223, 227, 231, 235, 239, 243, 247, 251, 255
            },
            new byte[] {
                0, 255, 16, 239, 32, 223, 48, 207, 64, 191, 80, 175, 96, 159, 112, 143,
                3, 252, 19, 236, 35, 220, 51, 204, 67, 188, 83, 172, 100, 155, 116, 139,
                6, 249, 22, 233, 38, 217, 54, 201, 71, 184, 87, 168, 103, 152, 119, 136,
                9, 246, 25, 230, 42, 213, 58, 197, 74, 181, 90, 165, 106, 149, 122, 133,
                13, 242, 29, 226, 45, 210, 61, 194, 77, 178, 93, 162, 109, 146, 125, 130
            },
            new byte[] {
                0, 255, 8, 247, 16, 239, 24, 231, 32, 223, 40, 215, 48, 207, 56, 199,
                64, 191, 72, 183, 80, 175, 88, 167, 96, 159, 104, 151, 112, 143, 120, 135,
                2, 253, 10, 245, 18, 237, 26, 229, 35, 220, 43, 212, 51, 204, 59, 196,
                67, 188, 75, 180, 83, 172, 91, 164, 99, 156, 107, 148, 115, 140, 123, 132,
                5, 250, 13, 242, 21, 234, 29, 226, 37, 218, 45, 210, 53, 202, 61, 194,
                70, 185, 78, 177, 86, 169, 94, 161, 102, 153, 110, 145, 118, 137, 126, 129
            },
            new byte[] {
                0, 2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30,
                32, 34, 36, 38, 40, 42, 44, 46, 48, 50, 52, 54, 56, 58, 60, 62,
                64, 66, 68, 70, 72, 74, 76, 78, 80, 82, 84, 86, 88, 90, 92, 94,
                96, 98, 100, 102, 104, 106, 108, 110, 112, 114, 116, 118, 120, 122, 124, 126,
                129, 131, 133, 135, 137, 139, 141, 143, 145, 147, 149, 151, 153, 155, 157, 159,
                161, 163, 165, 167, 169, 171, 173, 175, 177, 179, 181, 183, 185, 187, 189, 191,
                193, 195, 197, 199, 201, 203, 205, 207, 209, 211, 213, 215, 217, 219, 221, 223,
                225, 227, 229, 231, 233, 235, 237, 239, 241, 243, 245, 247, 249, 251, 253, 255
            },
            new byte[] {
                0, 255, 8, 247, 16, 239, 24, 231, 32, 223, 40, 215, 48, 207, 56, 199,
                64, 191, 72, 183, 80, 175, 88, 167, 96, 159, 104, 151, 112, 143, 120, 135,
                1, 254, 9, 246, 17, 238, 25, 230, 33, 222, 41, 214, 49, 206, 57, 198,
                65, 190, 73, 182, 81, 174, 89, 166, 97, 158, 105, 150, 113, 142, 121, 134,
                3, 252, 11, 244, 19, 236, 27, 228, 35, 220, 43, 212, 51, 204, 59, 196,
                67, 188, 75, 180, 83, 172, 91, 164, 99, 156, 107, 148, 115, 140, 123, 132,
                4, 251, 12, 243, 20, 235, 28, 227, 36, 219, 44, 211, 52, 203, 60, 195,
                68, 187, 76, 179, 84, 171, 92, 163, 100, 155, 108, 147, 116, 139, 124, 131,
                6, 249, 14, 241, 22, 233, 30, 225, 38, 217, 46, 209, 54, 201, 62, 193,
                70, 185, 78, 177, 86, 169, 94, 161, 102, 153, 110, 145, 118, 137, 126, 129
            },
            new byte[] {
                0, 255, 4, 251, 8, 247, 12, 243, 16, 239, 20, 235, 24, 231, 28, 227,
                32, 223, 36, 219, 40, 215, 44, 211, 48, 207, 52, 203, 56, 199, 60, 195,
                64, 191, 68, 187, 72, 183, 76, 179, 80, 175, 84, 171, 88, 167, 92, 163,
                96, 159, 100, 155, 104, 151, 108, 147, 112, 143, 116, 139, 120, 135, 124, 131,
                1, 254, 5, 250, 9, 246, 13, 242, 17, 238, 21, 234, 25, 230, 29, 226,
                33, 222, 37, 218, 41, 214, 45, 210, 49, 206, 53, 202, 57, 198, 61, 194,
                65, 190, 69, 186, 73, 182, 77, 178, 81, 174, 85, 170, 89, 166, 93, 162,
                97, 158, 101, 154, 105, 150, 109, 146, 113, 142, 117, 138, 121, 134, 125, 130,
                2, 253, 6, 249, 10, 245, 14, 241, 18, 237, 22, 233, 26, 229, 30, 225,
                34, 221, 38, 217, 42, 213, 46, 209, 50, 205, 54, 201, 58, 197, 62, 193,
                66, 189, 70, 185, 74, 181, 78, 177, 82, 173, 86, 169, 90, 165, 94, 161,
                98, 157, 102, 153, 106, 149, 110, 145, 114, 141, 118, 137, 122, 133, 126, 129
            },
            new byte[] {
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15,
                16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31,
                32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47,
                48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63,
                64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79,
                80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93, 94, 95,
                96, 97, 98, 99, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111,
                112, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 123, 124, 125, 126, 127,
                128, 129, 130, 131, 132, 133, 134, 135, 136, 137, 138, 139, 140, 141, 142, 143,
                144, 145, 146, 147, 148, 149, 150, 151, 152, 153, 154, 155, 156, 157, 158, 159,
                160, 161, 162, 163, 164, 165, 166, 167, 168, 169, 170, 171, 172, 173, 174, 175,
                176, 177, 178, 179, 180, 181, 182, 183, 184, 185, 186, 187, 188, 189, 190, 191,
                192, 193, 194, 195, 196, 197, 198, 199, 200, 201, 202, 203, 204, 205, 206, 207,
                208, 209, 210, 211, 212, 213, 214, 215, 216, 217, 218, 219, 220, 221, 222, 223,
                224, 225, 226, 227, 228, 229, 230, 231, 232, 233, 234, 235, 236, 237, 238, 239,
                240, 241, 242, 243, 244, 245, 246, 247, 248, 249, 250, 251, 252, 253, 254, 255
            }
        };

        private static readonly UInt4[] _errorValues =
        {
            Constants.ErrorValue,
            Constants.ErrorValue
        };

        public static UInt4[] DecodeColorEndpoints(byte[] values, ColorFormat format, int quantizationLevel)
        {
            /* HDR formats will contain values 0..0xFFFF (half-precision) */
            /* LDR formats will contain values 0..0xFF   (byte) */

            switch (format)
            {
                case ColorFormat.FmtLuminance:
                    return UnpackLuminance(values, quantizationLevel);

                case ColorFormat.FmtLuminanceDelta:
                    return UnpackLuminanceDelta(values, quantizationLevel);

                case ColorFormat.FmtHdrLuminanceLargeRange:
                    return UnpackHdrLuminanceLarge(values, quantizationLevel);

                case ColorFormat.FmtHdrLuminanceSmallRange:
                    return UnpackHdrLuminanceSmall(values, quantizationLevel);

                case ColorFormat.FmtLuminanceAlpha:
                    return UnpackLuminanceAlpha(values, quantizationLevel);

                case ColorFormat.FmtLuminanceAlphaDelta:
                    return UnpackLuminanceAlphaDelta(values, quantizationLevel);

                case ColorFormat.FmtRgbScale:
                    return UnpackRgbScale(values, quantizationLevel);

                case ColorFormat.FmtHdrRgbScale:
                    return UnpackHdrRgbScale(values, quantizationLevel);

                case ColorFormat.FmtRgb:
                    return UnpackRgb(values, quantizationLevel, out _);

                case ColorFormat.FmtRgbDelta:
                    return UnpackRgbDelta(values, quantizationLevel, out _);

                case ColorFormat.FmtRgbScaleAlpha:
                    return UnpackRgbScaleAlpha(values, quantizationLevel);

                case ColorFormat.FmtHdrRgb:
                    return UnpackHdrRgb(values, quantizationLevel);

                case ColorFormat.FmtRgba:
                    return UnpackRgba(values, quantizationLevel);

                case ColorFormat.FmtRgbaDelta:
                    return UnpackRgbaDelta(values, quantizationLevel);

                case ColorFormat.FmtHdrRgbLdrAlpha:
                    return UnpackHdrRgbLdrAlpha(values, quantizationLevel);

                case ColorFormat.FmtHdrRgba:
                    return UnpackHdrRgba(values, quantizationLevel);

                default:
                    return _errorValues;
                }
        }

        private static UInt4[] UnpackLuminance(byte[] values, int quantizationLevel)
        {
            var lum0 = ColorUnquantizationTable[quantizationLevel][values[0]];
            var lum1 = ColorUnquantizationTable[quantizationLevel][values[1]];
            return new[]
            {
                new UInt4(lum0, lum0, lum0,255),
                new UInt4(lum1, lum1, lum1,255)
            };
        }

        private static UInt4[] UnpackLuminanceDelta(byte[] values, int quantizationLevel)
        {
            var v0 = ColorUnquantizationTable[quantizationLevel][values[0]];
            var v1 = ColorUnquantizationTable[quantizationLevel][values[1]];
            var l0 = (v0 >> 2) | (v1 & 0xC0);
            var l1 = l0 + (v1 & 0x3F);

            if (l1 > 255)
                l1 = 255;

            return new[]
            {
                new UInt4(l0, l0, l0, 255),
                new UInt4(l1, l1, l1, 255)
            };
        }

        private static UInt4[] UnpackHdrLuminanceSmall(byte[] values, int quantizationLevel)
        {
            int v0 = ColorUnquantizationTable[quantizationLevel][values[0]];
            int v1 = ColorUnquantizationTable[quantizationLevel][values[1]];

            int y0, y1;
            if ((v0 & 0x80) != 0)
            {
                y0 = ((v1 & 0xE0) << 4) | ((v0 & 0x7F) << 2);
                y1 = (v1 & 0x1F) << 2;
            }
            else
            {
                y0 = ((v1 & 0xF0) << 4) | ((v0 & 0x7F) << 1);
                y1 = (v1 & 0xF) << 1;
            }

            y1 += y0;
            if (y1 > 0xFFF)
                y1 = 0xFFF;

            return new[]
            {
                new UInt4(y0<<4, y0<<4, y0<<4,0x7800),
                new UInt4(y1<<4, y1<<4, y1<<4,0x7800)
            };
        }

        private static UInt4[] UnpackHdrLuminanceLarge(byte[] values, int quantizationLevel)
        {
            int v0 = ColorUnquantizationTable[quantizationLevel][values[0]];
            int v1 = ColorUnquantizationTable[quantizationLevel][values[1]];

            int y0, y1;
            if (v1 >= v0)
            {
                y0 = v0 << 4;
                y1 = v1 << 4;
            }
            else
            {
                y0 = (v1 << 4) + 8;
                y1 = (v0 << 4) - 8;
            }

            return new[]
            {
                new UInt4(y0<<4, y0<<4, y0<<4,0x7800),
                new UInt4(y1<<4, y1<<4, y1<<4,0x7800)
            };
        }

        private static UInt4[] UnpackLuminanceAlpha(byte[] values, int quantizationLevel)
        {
            int lum0 = ColorUnquantizationTable[quantizationLevel][values[0]];
            int lum1 = ColorUnquantizationTable[quantizationLevel][values[1]];
            int alpha0 = ColorUnquantizationTable[quantizationLevel][values[2]];
            int alpha1 = ColorUnquantizationTable[quantizationLevel][values[3]];

            return new[]
            {
                new UInt4(lum0,lum0,lum0,alpha0),
                new UInt4(lum1,lum1,lum1,alpha1)
            };
        }

        private static UInt4[] UnpackLuminanceAlphaDelta(byte[] values, int quantizationLevel)
        {
            int lum0 = ColorUnquantizationTable[quantizationLevel][values[0]];
            int lum1 = ColorUnquantizationTable[quantizationLevel][values[1]];
            int alpha0 = ColorUnquantizationTable[quantizationLevel][values[2]];
            int alpha1 = ColorUnquantizationTable[quantizationLevel][values[3]];

            lum0 |= (lum1 & 0x80) << 1;
            alpha0 |= (alpha1 & 0x80) << 1;
            lum1 &= 0x7F;
            alpha1 &= 0x7F;
            if ((lum1 & 0x40) != 0)
                lum1 -= 0x80;
            if ((alpha1 & 0x40) != 0)
                alpha1 -= 0x80;

            lum0 >>= 1;
            lum1 >>= 1;
            alpha0 >>= 1;
            alpha1 >>= 1;
            lum1 += lum0;
            alpha1 += alpha0;

            if (lum1 < 0)
                lum1 = 0;
            else if (lum1 > 255)
                lum1 = 255;

            if (alpha1 < 0)
                alpha1 = 0;
            else if (alpha1 > 255)
                alpha1 = 255;

            return new[]
            {
                new UInt4(lum0,lum0,lum0,alpha0),
                new UInt4(lum1,lum1,lum1,alpha1)
            };
        }

        private static UInt4[] UnpackRgbScale(byte[] values, int quantizationLevel)
        {
            int ir = ColorUnquantizationTable[quantizationLevel][values[0]];
            int ig = ColorUnquantizationTable[quantizationLevel][values[1]];
            int ib = ColorUnquantizationTable[quantizationLevel][values[2]];

            int scale = ColorUnquantizationTable[quantizationLevel][values[3]];

            return new[]
            {
                new UInt4(ir,ig,ib,255),
                new UInt4((ir * scale) >> 8, (ig * scale) >> 8, (ib * scale) >> 8,255)
            };
        }

        private static UInt4[] UnpackHdrRgbScale(byte[] values, int quantizationLevel)
        {
            int v0 = ColorUnquantizationTable[quantizationLevel][values[0]];
            int v1 = ColorUnquantizationTable[quantizationLevel][values[1]];
            int v2 = ColorUnquantizationTable[quantizationLevel][values[2]];
            int v3 = ColorUnquantizationTable[quantizationLevel][values[3]];

            int modeval = ((v0 & 0xC0) >> 6) | (((v1 & 0x80) >> 7) << 2) | (((v2 & 0x80) >> 7) << 3);

            int majcomp;
            int mode;
            if ((modeval & 0xC) != 0xC)
            {
                majcomp = modeval >> 2;
                mode = modeval & 3;
            }
            else if (modeval != 0xF)
            {
                majcomp = modeval & 3;
                mode = 4;
            }
            else
            {
                majcomp = 0;
                mode = 5;
            }

            int red = v0 & 0x3F;
            int green = v1 & 0x1F;
            int blue = v2 & 0x1F;
            int scale = v3 & 0x1F;

            int bit0 = (v1 >> 6) & 1;
            int bit1 = (v1 >> 5) & 1;
            int bit2 = (v2 >> 6) & 1;
            int bit3 = (v2 >> 5) & 1;
            int bit4 = (v3 >> 7) & 1;
            int bit5 = (v3 >> 6) & 1;
            int bit6 = (v3 >> 5) & 1;

            int ohcomp = 1 << mode;

            if ((ohcomp & 0x30) != 0)
                green |= bit0 << 6;
            if ((ohcomp & 0x3A) != 0)
                green |= bit1 << 5;
            if ((ohcomp & 0x30) != 0)
                blue |= bit2 << 6;
            if ((ohcomp & 0x3A) != 0)
                blue |= bit3 << 5;

            if ((ohcomp & 0x3D) != 0)
                scale |= bit6 << 5;
            if ((ohcomp & 0x2D) != 0)
                scale |= bit5 << 6;
            if ((ohcomp & 0x04) != 0)
                scale |= bit4 << 7;

            if ((ohcomp & 0x3B) != 0)
                red |= bit4 << 6;
            if ((ohcomp & 0x04) != 0)
                red |= bit3 << 6;

            if ((ohcomp & 0x10) != 0)
                red |= bit5 << 7;
            if ((ohcomp & 0x0F) != 0)
                red |= bit2 << 7;

            if ((ohcomp & 0x05) != 0)
                red |= bit1 << 8;
            if ((ohcomp & 0x0A) != 0)
                red |= bit0 << 8;

            if ((ohcomp & 0x05) != 0)
                red |= bit0 << 9;
            if ((ohcomp & 0x02) != 0)
                red |= bit6 << 9;

            if ((ohcomp & 0x01) != 0)
                red |= bit3 << 10;
            if ((ohcomp & 0x02) != 0)
                red |= bit5 << 10;

            // expand to 12 bits.
            var shamts = new[] { 1, 1, 2, 3, 4, 5 };
            var shamt = shamts[mode];
            red <<= shamt;
            green <<= shamt;
            blue <<= shamt;
            scale <<= shamt;

            // on modes 0 to 4, the values stored for "green" and "blue" are differentials,
            // not absolute values.
            if (mode != 5)
            {
                green = red - green;
                blue = red - blue;
            }

            // switch around components.
            int temp;
            switch (majcomp)
            {
                case 1:
                    temp = red;
                    red = green;
                    green = temp;
                    break;
                case 2:
                    temp = red;
                    red = blue;
                    blue = temp;
                    break;
            }

            int red0 = red - scale;
            int green0 = green - scale;
            int blue0 = blue - scale;

            // clamp to [0,0xFFF].
            if (red < 0)
                red = 0;
            if (green < 0)
                green = 0;
            if (blue < 0)
                blue = 0;

            if (red0 < 0)
                red0 = 0;
            if (green0 < 0)
                green0 = 0;
            if (blue0 < 0)
                blue0 = 0;

            return new[]
            {
                new UInt4(red0 << 4, green0 << 4, blue0 << 4, 0x7800),
                new UInt4(red << 4, green << 4, blue << 4, 0x7800),
            };
        }

        private static UInt4[] UnpackRgb(byte[] values, int quantizationLevel, out bool order)
        {
            int ri0b = ColorUnquantizationTable[quantizationLevel][values[0]];
            int ri1b = ColorUnquantizationTable[quantizationLevel][values[1]];
            int gi0b = ColorUnquantizationTable[quantizationLevel][values[2]];
            int gi1b = ColorUnquantizationTable[quantizationLevel][values[3]];
            int bi0b = ColorUnquantizationTable[quantizationLevel][values[4]];
            int bi1b = ColorUnquantizationTable[quantizationLevel][values[5]];

            if (ri0b + gi0b + bi0b > ri1b + gi1b + bi1b)
            {
                // blue-contraction
                ri0b = (ri0b + bi0b) >> 1;
                gi0b = (gi0b + bi0b) >> 1;
                ri1b = (ri1b + bi1b) >> 1;
                gi1b = (gi1b + bi1b) >> 1;

                order = true;

                return new[]
                {
                    new UInt4(ri1b,gi1b,bi1b,255),
                    new UInt4(ri0b,gi0b,bi0b,255)
                };
            }
            else
            {
                order = false;

                return new[]
                {
                    new UInt4(ri0b,gi0b,bi0b,255),
                    new UInt4(ri1b,gi1b,bi1b,255)
                };
            }
        }

        private static UInt4[] UnpackRgbDelta(byte[] values, int quantizationLevel, out bool order)
        {
            int r0 = ColorUnquantizationTable[quantizationLevel][values[0]];
            int g0 = ColorUnquantizationTable[quantizationLevel][values[2]];
            int b0 = ColorUnquantizationTable[quantizationLevel][values[4]];

            int r1 = ColorUnquantizationTable[quantizationLevel][values[1]];
            int g1 = ColorUnquantizationTable[quantizationLevel][values[3]];
            int b1 = ColorUnquantizationTable[quantizationLevel][values[5]];

            // perform the bit-transfer procedure
            r0 |= (r1 & 0x80) << 1;
            g0 |= (g1 & 0x80) << 1;
            b0 |= (b1 & 0x80) << 1;
            r1 &= 0x7F;
            g1 &= 0x7F;
            b1 &= 0x7F;
            if ((r1 & 0x40) != 0)
                r1 -= 0x80;
            if ((g1 & 0x40) != 0)
                g1 -= 0x80;
            if ((b1 & 0x40) != 0)
                b1 -= 0x80;

            r0 >>= 1;
            g0 >>= 1;
            b0 >>= 1;
            r1 >>= 1;
            g1 >>= 1;
            b1 >>= 1;

            int rgbsum = r1 + g1 + b1;

            r1 += r0;
            g1 += g0;
            b1 += b0;

            int r0e, g0e, b0e;
            int r1e, g1e, b1e;

            if (rgbsum >= 0)
            {
                r0e = r0;
                g0e = g0;
                b0e = b0;

                r1e = r1;
                g1e = g1;
                b1e = b1;

                order = false;
            }
            else
            {
                r0e = (r1 + b1) >> 1;
                g0e = (g1 + b1) >> 1;
                b0e = b1;

                r1e = (r0 + b0) >> 1;
                g1e = (g0 + b0) >> 1;
                b1e = b0;

                order = true;
            }

            if (r0e < 0)
                r0e = 0;
            else if (r0e > 255)
                r0e = 255;

            if (g0e < 0)
                g0e = 0;
            else if (g0e > 255)
                g0e = 255;

            if (b0e < 0)
                b0e = 0;
            else if (b0e > 255)
                b0e = 255;

            if (r1e < 0)
                r1e = 0;
            else if (r1e > 255)
                r1e = 255;

            if (g1e < 0)
                g1e = 0;
            else if (g1e > 255)
                g1e = 255;

            if (b1e < 0)
                b1e = 0;
            else if (b1e > 255)
                b1e = 255;

            return new[]
            {
                new UInt4(r0e,g0e,b0e,255),
                new UInt4(r1e,g1e,b1e,255)
            };
        }

        private static UInt4[] UnpackRgbScaleAlpha(byte[] values, int quantizationLevel)
        {
            var rgbValues = UnpackRgbScale(values, quantizationLevel);

            rgbValues[0].w = ColorUnquantizationTable[quantizationLevel][values[4]];
            rgbValues[1].w = ColorUnquantizationTable[quantizationLevel][values[5]];

            return rgbValues;
        }

        private static UInt4[] UnpackHdrRgb(byte[] values, int quantizationLevel)
        {
            int v0 = ColorUnquantizationTable[quantizationLevel][values[0]];
            int v1 = ColorUnquantizationTable[quantizationLevel][values[1]];
            int v2 = ColorUnquantizationTable[quantizationLevel][values[2]];
            int v3 = ColorUnquantizationTable[quantizationLevel][values[3]];
            int v4 = ColorUnquantizationTable[quantizationLevel][values[4]];
            int v5 = ColorUnquantizationTable[quantizationLevel][values[5]];

            // extract all the fixed-placement bitfields
            int modeval = ((v1 & 0x80) >> 7) | (((v2 & 0x80) >> 7) << 1) | (((v3 & 0x80) >> 7) << 2);

            int majcomp = ((v4 & 0x80) >> 7) | (((v5 & 0x80) >> 7) << 1);

            if (majcomp == 3)
            {
                return new[]
                {
                    new UInt4(v0 << 8, v2 << 8, (v4 & 0x7F) << 9, 0x7800),
                    new UInt4(v1 << 8, v3 << 8, (v5 & 0x7F) << 9, 0x7800)
                };
            }

            int a = v0 | ((v1 & 0x40) << 2);
            int b0 = v2 & 0x3f;
            int b1 = v3 & 0x3f;
            int c = v1 & 0x3f;
            int d0 = v4 & 0x7f;
            int d1 = v5 & 0x7f;

            // get hold of the number of bits in 'd0' and 'd1'
            var dbitsTable = new[] { 7, 6, 7, 6, 5, 6, 5, 6 };
            int dbits = dbitsTable[modeval];

            // extract six variable-placement bits
            int bit0 = (v2 >> 6) & 1;
            int bit1 = (v3 >> 6) & 1;

            int bit2 = (v4 >> 6) & 1;
            int bit3 = (v5 >> 6) & 1;
            int bit4 = (v4 >> 5) & 1;
            int bit5 = (v5 >> 5) & 1;

            // and prepend the variable-placement bits depending on mode.
            int ohmod = 1 << modeval;   // one-hot-mode
            if ((ohmod & 0xA4) != 0)
                a |= bit0 << 9;
            if ((ohmod & 0x8) != 0)
                a |= bit2 << 9;
            if ((ohmod & 0x50) != 0)
                a |= bit4 << 9;

            if ((ohmod & 0x50) != 0)
                a |= bit5 << 10;
            if ((ohmod & 0xA0) != 0)
                a |= bit1 << 10;

            if ((ohmod & 0xC0) != 0)
                a |= bit2 << 11;

            if ((ohmod & 0x4) != 0)
                c |= bit1 << 6;
            if ((ohmod & 0xE8) != 0)
                c |= bit3 << 6;

            if ((ohmod & 0x20) != 0)
                c |= bit2 << 7;

            if ((ohmod & 0x5B) != 0)
                b0 |= bit0 << 6;
            if ((ohmod & 0x5B) != 0)
                b1 |= bit1 << 6;

            if ((ohmod & 0x12) != 0)
                b0 |= bit2 << 7;
            if ((ohmod & 0x12) != 0)
                b1 |= bit3 << 7;

            if ((ohmod & 0xAF) != 0)
                d0 |= bit4 << 5;
            if ((ohmod & 0xAF) != 0)
                d1 |= bit5 << 5;
            if ((ohmod & 0x5) != 0)
                d0 |= bit2 << 6;
            if ((ohmod & 0x5) != 0)
                d1 |= bit3 << 6;

            // sign-extend 'd0' and 'd1'
            // note: this code assumes that signed right-shift actually sign-fills, not zero-fills.
            var d0x = d0;
            var d1x = d1;
            int sx_shamt = 32 - dbits;
            d0x <<= sx_shamt;
            d0x >>= sx_shamt;
            d1x <<= sx_shamt;
            d1x >>= sx_shamt;
            d0 = d0x;
            d1 = d1x;

            // expand all values to 12 bits, with left-shift as needed.
            int val_shamt = (modeval >> 1) ^ 3;
            a <<= val_shamt;
            b0 <<= val_shamt;
            b1 <<= val_shamt;
            c <<= val_shamt;
            d0 <<= val_shamt;
            d1 <<= val_shamt;

            // then compute the actual color values.
            int red1 = a;
            int green1 = a - b0;
            int blue1 = a - b1;
            int red0 = a - c;
            int green0 = a - b0 - c - d0;
            int blue0 = a - b1 - c - d1;

            // clamp the color components to [0,2^12 - 1]
            if (red0 < 0)
                red0 = 0;
            else if (red0 > 0xFFF)
                red0 = 0xFFF;

            if (green0 < 0)
                green0 = 0;
            else if (green0 > 0xFFF)
                green0 = 0xFFF;

            if (blue0 < 0)
                blue0 = 0;
            else if (blue0 > 0xFFF)
                blue0 = 0xFFF;

            if (red1 < 0)
                red1 = 0;
            else if (red1 > 0xFFF)
                red1 = 0xFFF;

            if (green1 < 0)
                green1 = 0;
            else if (green1 > 0xFFF)
                green1 = 0xFFF;

            if (blue1 < 0)
                blue1 = 0;
            else if (blue1 > 0xFFF)
                blue1 = 0xFFF;

            // switch around the color components
            int temp0, temp1;
            switch (majcomp)
            {
                case 1:                 // switch around red and green
                    temp0 = red0;
                    temp1 = red1;
                    red0 = green0;
                    red1 = green1;
                    green0 = temp0;
                    green1 = temp1;
                    break;

                case 2:                 // switch around red and blue
                    temp0 = red0;
                    temp1 = red1;
                    red0 = blue0;
                    red1 = blue1;
                    blue0 = temp0;
                    blue1 = temp1;
                    break;

                case 0:                 // no switch
                    break;
            }

            return new[]
            {
                new UInt4(red0 << 4, green0 << 4, blue0 << 4, 0x7800),
                new UInt4(red1 << 4, green1 << 4, blue1 << 4, 0x7800)
            };
        }

        private static UInt4[] UnpackRgba(byte[] values, int quantizationLevel)
        {
            var rgbValues = UnpackRgb(values, quantizationLevel, out var order);

            if (order)
            {
                rgbValues[0].w = ColorUnquantizationTable[quantizationLevel][values[7]];
                rgbValues[1].w = ColorUnquantizationTable[quantizationLevel][values[6]];
            }
            else
            {
                rgbValues[0].w = ColorUnquantizationTable[quantizationLevel][values[6]];
                rgbValues[1].w = ColorUnquantizationTable[quantizationLevel][values[7]];
            }

            return rgbValues;
        }

        private static UInt4[] UnpackRgbaDelta(byte[] values, int quantizationLevel)
        {
            int a0 = ColorUnquantizationTable[quantizationLevel][values[6]];
            int a1 = ColorUnquantizationTable[quantizationLevel][values[7]];
            a0 |= (a1 & 0x80) << 1;
            a1 &= 0x7F;
            if ((a1 & 0x40) != 0)
                a1 -= 0x80;
            a0 >>= 1;
            a1 >>= 1;
            a1 += a0;

            if (a1 < 0)
                a1 = 0;
            else if (a1 > 255)
                a1 = 255;

            var rgbValues = UnpackRgbDelta(values, quantizationLevel, out var order);

            if (order)
            {
                rgbValues[0].w = a1;
                rgbValues[1].w = a0;
            }
            else
            {
                rgbValues[0].w = a0;
                rgbValues[1].w = a1;
            }

            return rgbValues;
        }

        private static UInt4[] UnpackHdrRgbLdrAlpha(byte[] values, int quantizationLevel)
        {
            var rgbValues = UnpackHdrRgb(values, quantizationLevel);

            int v6 = ColorUnquantizationTable[quantizationLevel][values[6]];
            int v7 = ColorUnquantizationTable[quantizationLevel][values[7]];

            rgbValues[0].w = v6;
            rgbValues[1].w = v7;

            return rgbValues;
        }

        private static UInt4[] UnpackHdrRgba(byte[] values, int quantizationLevel)
        {
            var rgbValues = UnpackHdrRgb(values, quantizationLevel);

            var alphaValues = UnpackHdrAlpha(new[] { values[6], values[7] }, quantizationLevel);

            rgbValues[0].w = alphaValues[0];
            rgbValues[1].w = alphaValues[1];

            return rgbValues;
        }

        private static int[] UnpackHdrAlpha(byte[] values, int quantizationLevel)
        {
            int v6 = ColorUnquantizationTable[quantizationLevel][values[0]];
            int v7 = ColorUnquantizationTable[quantizationLevel][values[1]];

            int selector = ((v6 >> 7) & 1) | ((v7 >> 6) & 2);
            v6 &= 0x7F;
            v7 &= 0x7F;
            if (selector == 3)
            {
                return new[] { v6 << 9, v7 << 9 };
            }

            v6 |= (v7 << (selector + 1)) & 0x780;
            v7 &= (0x3f >> selector);
            v7 ^= 32 >> selector;
            v7 -= 32 >> selector;
            v6 <<= (4 - selector);
            v7 <<= (4 - selector);
            v7 += v6;

            if (v7 < 0)
                v7 = 0;
            else if (v7 > 0xFFF)
                v7 = 0xFFF;

            return new[] { v6 << 4, v7 << 4 };
        }
    }
}
