using Komponent.IO;

namespace Kanvas.Encoding.BlockCompressions.ASTC_CS
{
    internal static class IntegerSequenceEncoding
    {
        static readonly byte[][] _quintsOfInteger = {
            new byte[] {0, 0, 0},  new byte[] {1, 0, 0},  new byte[] {2, 0, 0},  new byte[] {3, 0, 0},
            new byte[] {4, 0, 0},  new byte[] {0, 4, 0},  new byte[] {4, 4, 0},  new byte[] {4, 4, 4},
            new byte[] {0, 1, 0},  new byte[] {1, 1, 0},  new byte[] {2, 1, 0},  new byte[] {3, 1, 0},
            new byte[] {4, 1, 0},  new byte[] {1, 4, 0},  new byte[] {4, 4, 1},  new byte[] {4, 4, 4},
            new byte[] {0, 2, 0},  new byte[] {1, 2, 0},  new byte[] {2, 2, 0},  new byte[] {3, 2, 0},
            new byte[] {4, 2, 0},  new byte[] {2, 4, 0},  new byte[] {4, 4, 2},  new byte[] {4, 4, 4},
            new byte[] {0, 3, 0},  new byte[] {1, 3, 0},  new byte[] {2, 3, 0},  new byte[] {3, 3, 0},
            new byte[] {4, 3, 0},  new byte[] {3, 4, 0},  new byte[] {4, 4, 3},  new byte[] {4, 4, 4},
            new byte[] {0, 0, 1},  new byte[] {1, 0, 1},  new byte[] {2, 0, 1},  new byte[] {3, 0, 1},
            new byte[] {4, 0, 1},  new byte[] {0, 4, 1},  new byte[] {4, 0, 4},  new byte[] {0, 4, 4},
            new byte[] {0, 1, 1},  new byte[] {1, 1, 1},  new byte[] {2, 1, 1},  new byte[] {3, 1, 1},
            new byte[] {4, 1, 1},  new byte[] {1, 4, 1},  new byte[] {4, 1, 4},  new byte[] {1, 4, 4},
            new byte[] {0, 2, 1},  new byte[] {1, 2, 1},  new byte[] {2, 2, 1},  new byte[] {3, 2, 1},
            new byte[] {4, 2, 1},  new byte[] {2, 4, 1},  new byte[] {4, 2, 4},  new byte[] {2, 4, 4},
            new byte[] {0, 3, 1},  new byte[] {1, 3, 1},  new byte[] {2, 3, 1},  new byte[] {3, 3, 1},
            new byte[] {4, 3, 1},  new byte[] {3, 4, 1},  new byte[] {4, 3, 4},  new byte[] {3, 4, 4},
            new byte[] {0, 0, 2},  new byte[] {1, 0, 2},  new byte[] {2, 0, 2},  new byte[] {3, 0, 2},
            new byte[] {4, 0, 2},  new byte[] {0, 4, 2},  new byte[] {2, 0, 4},  new byte[] {3, 0, 4},
            new byte[] {0, 1, 2},  new byte[] {1, 1, 2},  new byte[] {2, 1, 2},  new byte[] {3, 1, 2},
            new byte[] {4, 1, 2},  new byte[] {1, 4, 2},  new byte[] {2, 1, 4},  new byte[] {3, 1, 4},
            new byte[] {0, 2, 2},  new byte[] {1, 2, 2},  new byte[] {2, 2, 2},  new byte[] {3, 2, 2},
            new byte[] {4, 2, 2},  new byte[] {2, 4, 2},  new byte[] {2, 2, 4},  new byte[] {3, 2, 4},
            new byte[] {0, 3, 2},  new byte[] {1, 3, 2},  new byte[] {2, 3, 2},  new byte[] {3, 3, 2},
            new byte[] {4, 3, 2},  new byte[] {3, 4, 2},  new byte[] {2, 3, 4},  new byte[] {3, 3, 4},
            new byte[] {0, 0, 3},  new byte[] {1, 0, 3},  new byte[] {2, 0, 3},  new byte[] {3, 0, 3},
            new byte[] {4, 0, 3},  new byte[] {0, 4, 3},  new byte[] {0, 0, 4},  new byte[] {1, 0, 4},
            new byte[] {0, 1, 3},  new byte[] {1, 1, 3},  new byte[] {2, 1, 3},  new byte[] {3, 1, 3},
            new byte[] {4, 1, 3},  new byte[] {1, 4, 3},  new byte[] {0, 1, 4},  new byte[] {1, 1, 4},
            new byte[] {0, 2, 3},  new byte[] {1, 2, 3},  new byte[] {2, 2, 3},  new byte[] {3, 2, 3},
            new byte[] {4, 2, 3},  new byte[] {2, 4, 3},  new byte[] {0, 2, 4},  new byte[] {1, 2, 4},
            new byte[] {0, 3, 3},  new byte[] {1, 3, 3},  new byte[] {2, 3, 3},  new byte[] {3, 3, 3},
            new byte[] {4, 3, 3},  new byte[] {3, 4, 3},  new byte[] {0, 3, 4},  new byte[] {1, 3, 4}
        };

        // packed quint-value for every unpacked quint-triplet
        // indexed by [high][middle][low]
        static readonly byte[][][] _integerOfQuint = {
            new [] {
                new byte[] {0, 1, 2, 3, 4},
                new byte[] {8, 9, 10, 11, 12},
                new byte[] {16, 17, 18, 19, 20},
                new byte[] {24, 25, 26, 27, 28},
                new byte[] {5, 13, 21, 29, 6}
            },
            new[] {
                new byte[] {32, 33, 34, 35, 36},
                new byte[] {40, 41, 42, 43, 44},
                new byte[] {48, 49, 50, 51, 52},
                new byte[] {56, 57, 58, 59, 60},
                new byte[] {37, 45, 53, 61, 14}
            },
            new[] {
                new byte[] {64, 65, 66, 67, 68},
                new byte[] {72, 73, 74, 75, 76},
                new byte[] {80, 81, 82, 83, 84},
                new byte[] {88, 89, 90, 91, 92},
                new byte[] {69, 77, 85, 93, 22}
            },
            new[] {
                new byte[] {96, 97, 98, 99, 100},
                new byte[] {104, 105, 106, 107, 108},
                new byte[] {112, 113, 114, 115, 116},
                new byte[] {120, 121, 122, 123, 124},
                new byte[] {101, 109, 117, 125, 30}
            },
            new[] {
                new byte[] {102, 103, 70, 71, 38},
                new byte[] {110, 111, 78, 79, 46},
                new byte[] {118, 119, 86, 87, 54},
                new byte[] {126, 127, 94, 95, 62},
                new byte[] {39, 47, 55, 63, 31}
            }
        };

        // unpacked trit quintuplets <low,_,_,_,high> for each packed-quint value
        static readonly byte[][] _tritsOfInteger = {
            new byte[] {0, 0, 0, 0, 0}, new byte[] {1, 0, 0, 0, 0}, new byte[] {2, 0, 0, 0, 0}, new byte[] {0, 0, 2, 0, 0},
            new byte[] {0, 1, 0, 0, 0}, new byte[] {1, 1, 0, 0, 0}, new byte[] {2, 1, 0, 0, 0}, new byte[] {1, 0, 2, 0, 0},
            new byte[] {0, 2, 0, 0, 0}, new byte[] {1, 2, 0, 0, 0}, new byte[] {2, 2, 0, 0, 0}, new byte[] {2, 0, 2, 0, 0},
            new byte[] {0, 2, 2, 0, 0}, new byte[] {1, 2, 2, 0, 0}, new byte[] {2, 2, 2, 0, 0}, new byte[] {2, 0, 2, 0, 0},
            new byte[] {0, 0, 1, 0, 0}, new byte[] {1, 0, 1, 0, 0}, new byte[] {2, 0, 1, 0, 0}, new byte[] {0, 1, 2, 0, 0},
            new byte[] {0, 1, 1, 0, 0}, new byte[] {1, 1, 1, 0, 0}, new byte[] {2, 1, 1, 0, 0}, new byte[] {1, 1, 2, 0, 0},
            new byte[] {0, 2, 1, 0, 0}, new byte[] {1, 2, 1, 0, 0}, new byte[] {2, 2, 1, 0, 0}, new byte[] {2, 1, 2, 0, 0},
            new byte[] {0, 0, 0, 2, 2}, new byte[] {1, 0, 0, 2, 2}, new byte[] {2, 0, 0, 2, 2}, new byte[] {0, 0, 2, 2, 2},
            new byte[] {0, 0, 0, 1, 0}, new byte[] {1, 0, 0, 1, 0}, new byte[] {2, 0, 0, 1, 0}, new byte[] {0, 0, 2, 1, 0},
            new byte[] {0, 1, 0, 1, 0}, new byte[] {1, 1, 0, 1, 0}, new byte[] {2, 1, 0, 1, 0}, new byte[] {1, 0, 2, 1, 0},
            new byte[] {0, 2, 0, 1, 0}, new byte[] {1, 2, 0, 1, 0}, new byte[] {2, 2, 0, 1, 0}, new byte[] {2, 0, 2, 1, 0},
            new byte[] {0, 2, 2, 1, 0}, new byte[] {1, 2, 2, 1, 0}, new byte[] {2, 2, 2, 1, 0}, new byte[] {2, 0, 2, 1, 0},
            new byte[] {0, 0, 1, 1, 0}, new byte[] {1, 0, 1, 1, 0}, new byte[] {2, 0, 1, 1, 0}, new byte[] {0, 1, 2, 1, 0},
            new byte[] {0, 1, 1, 1, 0}, new byte[] {1, 1, 1, 1, 0}, new byte[] {2, 1, 1, 1, 0}, new byte[] {1, 1, 2, 1, 0},
            new byte[] {0, 2, 1, 1, 0}, new byte[] {1, 2, 1, 1, 0}, new byte[] {2, 2, 1, 1, 0}, new byte[] {2, 1, 2, 1, 0},
            new byte[] {0, 1, 0, 2, 2}, new byte[] {1, 1, 0, 2, 2}, new byte[] {2, 1, 0, 2, 2}, new byte[] {1, 0, 2, 2, 2},
            new byte[] {0, 0, 0, 2, 0}, new byte[] {1, 0, 0, 2, 0}, new byte[] {2, 0, 0, 2, 0}, new byte[] {0, 0, 2, 2, 0},
            new byte[] {0, 1, 0, 2, 0}, new byte[] {1, 1, 0, 2, 0}, new byte[] {2, 1, 0, 2, 0}, new byte[] {1, 0, 2, 2, 0},
            new byte[] {0, 2, 0, 2, 0}, new byte[] {1, 2, 0, 2, 0}, new byte[] {2, 2, 0, 2, 0}, new byte[] {2, 0, 2, 2, 0},
            new byte[] {0, 2, 2, 2, 0}, new byte[] {1, 2, 2, 2, 0}, new byte[] {2, 2, 2, 2, 0}, new byte[] {2, 0, 2, 2, 0},
            new byte[] {0, 0, 1, 2, 0}, new byte[] {1, 0, 1, 2, 0}, new byte[] {2, 0, 1, 2, 0}, new byte[] {0, 1, 2, 2, 0},
            new byte[] {0, 1, 1, 2, 0}, new byte[] {1, 1, 1, 2, 0}, new byte[] {2, 1, 1, 2, 0}, new byte[] {1, 1, 2, 2, 0},
            new byte[] {0, 2, 1, 2, 0}, new byte[] {1, 2, 1, 2, 0}, new byte[] {2, 2, 1, 2, 0}, new byte[] {2, 1, 2, 2, 0},
            new byte[] {0, 2, 0, 2, 2}, new byte[] {1, 2, 0, 2, 2}, new byte[] {2, 2, 0, 2, 2}, new byte[] {2, 0, 2, 2, 2},
            new byte[] {0, 0, 0, 0, 2}, new byte[] {1, 0, 0, 0, 2}, new byte[] {2, 0, 0, 0, 2}, new byte[] {0, 0, 2, 0, 2},
            new byte[] {0, 1, 0, 0, 2}, new byte[] {1, 1, 0, 0, 2}, new byte[] {2, 1, 0, 0, 2}, new byte[] {1, 0, 2, 0, 2},
            new byte[] {0, 2, 0, 0, 2}, new byte[] {1, 2, 0, 0, 2}, new byte[] {2, 2, 0, 0, 2}, new byte[] {2, 0, 2, 0, 2},
            new byte[] {0, 2, 2, 0, 2}, new byte[] {1, 2, 2, 0, 2}, new byte[] {2, 2, 2, 0, 2}, new byte[] {2, 0, 2, 0, 2},
            new byte[] {0, 0, 1, 0, 2}, new byte[] {1, 0, 1, 0, 2}, new byte[] {2, 0, 1, 0, 2}, new byte[] {0, 1, 2, 0, 2},
            new byte[] {0, 1, 1, 0, 2}, new byte[] {1, 1, 1, 0, 2}, new byte[] {2, 1, 1, 0, 2}, new byte[] {1, 1, 2, 0, 2},
            new byte[] {0, 2, 1, 0, 2}, new byte[] {1, 2, 1, 0, 2}, new byte[] {2, 2, 1, 0, 2}, new byte[] {2, 1, 2, 0, 2},
            new byte[] {0, 2, 2, 2, 2}, new byte[] {1, 2, 2, 2, 2}, new byte[] {2, 2, 2, 2, 2}, new byte[] {2, 0, 2, 2, 2},
            new byte[] {0, 0, 0, 0, 1}, new byte[] {1, 0, 0, 0, 1}, new byte[] {2, 0, 0, 0, 1}, new byte[] {0, 0, 2, 0, 1},
            new byte[] {0, 1, 0, 0, 1}, new byte[] {1, 1, 0, 0, 1}, new byte[] {2, 1, 0, 0, 1}, new byte[] {1, 0, 2, 0, 1},
            new byte[] {0, 2, 0, 0, 1}, new byte[] {1, 2, 0, 0, 1}, new byte[] {2, 2, 0, 0, 1}, new byte[] {2, 0, 2, 0, 1},
            new byte[] {0, 2, 2, 0, 1}, new byte[] {1, 2, 2, 0, 1}, new byte[] {2, 2, 2, 0, 1}, new byte[] {2, 0, 2, 0, 1},
            new byte[] {0, 0, 1, 0, 1}, new byte[] {1, 0, 1, 0, 1}, new byte[] {2, 0, 1, 0, 1}, new byte[] {0, 1, 2, 0, 1},
            new byte[] {0, 1, 1, 0, 1}, new byte[] {1, 1, 1, 0, 1}, new byte[] {2, 1, 1, 0, 1}, new byte[] {1, 1, 2, 0, 1},
            new byte[] {0, 2, 1, 0, 1}, new byte[] {1, 2, 1, 0, 1}, new byte[] {2, 2, 1, 0, 1}, new byte[] {2, 1, 2, 0, 1},
            new byte[] {0, 0, 1, 2, 2}, new byte[] {1, 0, 1, 2, 2}, new byte[] {2, 0, 1, 2, 2}, new byte[] {0, 1, 2, 2, 2},
            new byte[] {0, 0, 0, 1, 1}, new byte[] {1, 0, 0, 1, 1}, new byte[] {2, 0, 0, 1, 1}, new byte[] {0, 0, 2, 1, 1},
            new byte[] {0, 1, 0, 1, 1}, new byte[] {1, 1, 0, 1, 1}, new byte[] {2, 1, 0, 1, 1}, new byte[] {1, 0, 2, 1, 1},
            new byte[] {0, 2, 0, 1, 1}, new byte[] {1, 2, 0, 1, 1}, new byte[] {2, 2, 0, 1, 1}, new byte[] {2, 0, 2, 1, 1},
            new byte[] {0, 2, 2, 1, 1}, new byte[] {1, 2, 2, 1, 1}, new byte[] {2, 2, 2, 1, 1}, new byte[] {2, 0, 2, 1, 1},
            new byte[] {0, 0, 1, 1, 1}, new byte[] {1, 0, 1, 1, 1}, new byte[] {2, 0, 1, 1, 1}, new byte[] {0, 1, 2, 1, 1},
            new byte[] {0, 1, 1, 1, 1}, new byte[] {1, 1, 1, 1, 1}, new byte[] {2, 1, 1, 1, 1}, new byte[] {1, 1, 2, 1, 1},
            new byte[] {0, 2, 1, 1, 1}, new byte[] {1, 2, 1, 1, 1}, new byte[] {2, 2, 1, 1, 1}, new byte[] {2, 1, 2, 1, 1},
            new byte[] {0, 1, 1, 2, 2}, new byte[] {1, 1, 1, 2, 2}, new byte[] {2, 1, 1, 2, 2}, new byte[] {1, 1, 2, 2, 2},
            new byte[] {0, 0, 0, 2, 1}, new byte[] {1, 0, 0, 2, 1}, new byte[] {2, 0, 0, 2, 1}, new byte[] {0, 0, 2, 2, 1},
            new byte[] {0, 1, 0, 2, 1}, new byte[] {1, 1, 0, 2, 1}, new byte[] {2, 1, 0, 2, 1}, new byte[] {1, 0, 2, 2, 1},
            new byte[] {0, 2, 0, 2, 1}, new byte[] {1, 2, 0, 2, 1}, new byte[] {2, 2, 0, 2, 1}, new byte[] {2, 0, 2, 2, 1},
            new byte[] {0, 2, 2, 2, 1}, new byte[] {1, 2, 2, 2, 1}, new byte[] {2, 2, 2, 2, 1}, new byte[] {2, 0, 2, 2, 1},
            new byte[] {0, 0, 1, 2, 1}, new byte[] {1, 0, 1, 2, 1}, new byte[] {2, 0, 1, 2, 1}, new byte[] {0, 1, 2, 2, 1},
            new byte[] {0, 1, 1, 2, 1}, new byte[] {1, 1, 1, 2, 1}, new byte[] {2, 1, 1, 2, 1}, new byte[] {1, 1, 2, 2, 1},
            new byte[] {0, 2, 1, 2, 1}, new byte[] {1, 2, 1, 2, 1}, new byte[] {2, 2, 1, 2, 1}, new byte[] {2, 1, 2, 2, 1},
            new byte[] {0, 2, 1, 2, 2}, new byte[] {1, 2, 1, 2, 2}, new byte[] {2, 2, 1, 2, 2}, new byte[] {2, 1, 2, 2, 2},
            new byte[] {0, 0, 0, 1, 2}, new byte[] {1, 0, 0, 1, 2}, new byte[] {2, 0, 0, 1, 2}, new byte[] {0, 0, 2, 1, 2},
            new byte[] {0, 1, 0, 1, 2}, new byte[] {1, 1, 0, 1, 2}, new byte[] {2, 1, 0, 1, 2}, new byte[] {1, 0, 2, 1, 2},
            new byte[] {0, 2, 0, 1, 2}, new byte[] {1, 2, 0, 1, 2}, new byte[] {2, 2, 0, 1, 2}, new byte[] {2, 0, 2, 1, 2},
            new byte[] {0, 2, 2, 1, 2}, new byte[] {1, 2, 2, 1, 2}, new byte[] {2, 2, 2, 1, 2}, new byte[] {2, 0, 2, 1, 2},
            new byte[] {0, 0, 1, 1, 2}, new byte[] {1, 0, 1, 1, 2}, new byte[] {2, 0, 1, 1, 2}, new byte[] {0, 1, 2, 1, 2},
            new byte[] {0, 1, 1, 1, 2}, new byte[] {1, 1, 1, 1, 2}, new byte[] {2, 1, 1, 1, 2}, new byte[] {1, 1, 2, 1, 2},
            new byte[] {0, 2, 1, 1, 2}, new byte[] {1, 2, 1, 1, 2}, new byte[] {2, 2, 1, 1, 2}, new byte[] {2, 1, 2, 1, 2},
            new byte[] {0, 2, 2, 2, 2}, new byte[] {1, 2, 2, 2, 2}, new byte[] {2, 2, 2, 2, 2}, new byte[] {2, 1, 2, 2, 2}
        };

        // packed trit-value for every unpacked trit-quintuplet
        // indexed by [high][][][][low]
        static readonly byte[][][][][] _integerOfTrit = {
            new[] {
                new[] {
                    new[] {
                        new byte[] {0, 1, 2},
                        new byte[] {4, 5, 6},
                        new byte[] {8, 9, 10}
                    },
                    new[] {
                        new byte[] {16, 17, 18},
                        new byte[] {20, 21, 22},
                        new byte[] {24, 25, 26}
                    },
                    new[] {
                        new byte[] {3, 7, 15},
                        new byte[] {19, 23, 27},
                        new byte[] {12, 13, 14}
                    }
                },
                new[] {
                    new[] {
                        new byte[] {32, 33, 34},
                        new byte[] {36, 37, 38},
                        new byte[] {40, 41, 42}
                    },
                    new[] {
                        new byte[] {48, 49, 50},
                        new byte[] {52, 53, 54},
                        new byte[] {56, 57, 58}
                    },
                    new[] {
                        new byte[] {35, 39, 47},
                        new byte[] {51, 55, 59},
                        new byte[] {44, 45, 46}
                    }
                },
                new[] {
                    new[] {
                        new byte[] {64, 65, 66},
                        new byte[] {68, 69, 70},
                        new byte[] {72, 73, 74}
                    },
                    new[] {
                        new byte[] {80, 81, 82},
                        new byte[] {84, 85, 86},
                        new byte[] {88, 89, 90}
                    },
                    new[] {
                        new byte[] {67, 71, 79},
                        new byte[] {83, 87, 91},
                        new byte[] {76, 77, 78}
                    }
                }
            },
            new[] {
                new[] {
                    new [] {
                        new byte[] {128, 129, 130},
                        new byte[] {132, 133, 134},
                        new byte[] {136, 137, 138}
                    },
                    new[] {
                        new byte[] {144, 145, 146},
                        new byte[] {148, 149, 150},
                        new byte[] {152, 153, 154}
                    },
                    new[] {
                        new byte[] {131, 135, 143},
                        new byte[] {147, 151, 155},
                        new byte[] {140, 141, 142}
                    }
                },
                new[] {
                    new[] {
                        new byte[] {160, 161, 162},
                        new byte[] {164, 165, 166},
                        new byte[] {168, 169, 170}
                    },
                    new[] {
                        new byte[] {176, 177, 178},
                        new byte[] {180, 181, 182},
                        new byte[] {184, 185, 186}
                    },
                    new[] {
                        new byte[] {163, 167, 175},
                        new byte[] {179, 183, 187},
                        new byte[] {172, 173, 174}
                    }
                },
                new[] {
                    new[] {
                        new byte[] {192, 193, 194},
                        new byte[] {196, 197, 198},
                        new byte[] {200, 201, 202}
                    },
                    new[] {
                        new byte[] {208, 209, 210},
                        new byte[] {212, 213, 214},
                        new byte[] {216, 217, 218}
                    },
                    new[] {
                        new byte[] {195, 199, 207},
                        new byte[] {211, 215, 219},
                        new byte[] {204, 205, 206}
                    }
                }
            },
            new[] {
                new[] {
                    new[] {
                        new byte[] {96, 97, 98},
                        new byte[] {100, 101, 102},
                        new byte[] {104, 105, 106}
                    },
                    new[] {
                        new byte[] {112, 113, 114},
                        new byte[] {116, 117, 118},
                        new byte[] {120, 121, 122}
                    },
                    new[] {
                        new byte[] {99, 103, 111},
                        new byte[] {115, 119, 123},
                        new byte[] {108, 109, 110}
                    }
                },
                new[] {
                    new[] {
                        new byte[] {224, 225, 226},
                        new byte[] {228, 229, 230},
                        new byte[] {232, 233, 234}
                    },
                    new[] {
                        new byte[] {240, 241, 242},
                        new byte[] {244, 245, 246},
                        new byte[] {248, 249, 250}
                    },
                    new[] {
                        new byte[] {227, 231, 239},
                        new byte[] {243, 247, 251},
                        new byte[] {236, 237, 238}
                    }
                },
                new[] {
                    new[] {
                        new byte[] {28, 29, 30},
                        new byte[] {60, 61, 62},
                        new byte[] {92, 93, 94}
                    },
                    new[] {
                        new byte[] {156, 157, 158},
                        new byte[] {188, 189, 190},
                        new byte[] {220, 221, 222}
                    },
                    new[] {
                        new byte[] {31, 63, 127},
                        new byte[] {159, 191, 255},
                        new byte[] {252, 253, 254}
                    }
                }
            }
        };

        static readonly byte[] tritsBits = { 2, 2, 1, 2, 1 };
        static readonly byte[] tritsBlockShift = { 0, 2, 4, 5, 7 };
        static readonly byte[] tritsNextLCounter = { 1, 2, 3, 4, 0 };
        static readonly byte[] tritsHCounterIncrement = { 0, 0, 0, 0, 1 };

        static readonly byte[] quintsBits = { 3, 2, 2 };
        static readonly byte[] quintsBlockShift = { 0, 3, 5 };
        static readonly byte[] quintsNextLCounter = { 1, 2, 0 };
        static readonly byte[] quintsHCounterIncrement = { 0, 0, 1 };

        public static byte[] Decode(BitReader br, int quantizationLevel, int elements)
        {
            var results = new byte[68];
            var tqBlocks = new byte[22];
            var (bits, trits, quints) = GetNumberOfUnits(quantizationLevel);

            var lCounter = 0;
            var hCounter = 0;
            for (var i = 0; i < elements; i++)
            {
                results[i] |= br.ReadBits<byte>(bits);

                if (trits > 0)
                {
                    var tData = br.ReadBits<byte>(tritsBits[lCounter]);
                    tqBlocks[hCounter] |= (byte)(tData << tritsBlockShift[lCounter]);
                    hCounter += tritsHCounterIncrement[lCounter];
                    lCounter = tritsNextLCounter[lCounter];
                }

                if (quints > 0)
                {
                    var tData = br.ReadBits<byte>(quintsBits[lCounter]);
                    tqBlocks[hCounter] |= (byte)(tData << quintsBlockShift[lCounter]);
                    hCounter += quintsHCounterIncrement[lCounter];
                    lCounter = quintsNextLCounter[lCounter];
                }
            }

            // unpack trit-blocks or quint-blocks as needed
            if (trits > 0)
            {
                var tritBlocks = (elements + 4) / 5;
                for (var i = 0; i < tritBlocks; i++)
                {
                    var unpackedTrits = _tritsOfInteger[tqBlocks[i]];
                    results[5 * i] |= (byte)(unpackedTrits[0] << bits);
                    results[5 * i + 1] |= (byte)(unpackedTrits[1] << bits);
                    results[5 * i + 2] |= (byte)(unpackedTrits[2] << bits);
                    results[5 * i + 3] |= (byte)(unpackedTrits[3] << bits);
                    results[5 * i + 4] |= (byte)(unpackedTrits[4] << bits);
                }
            }

            if (quints > 0)
            {
                var quintBlocks = (elements + 2) / 3;
                for (var i = 0; i < quintBlocks; i++)
                {
                    var unpackedQuints = _quintsOfInteger[tqBlocks[i]];
                    results[3 * i] |= (byte)(unpackedQuints[0] << bits);
                    results[3 * i + 1] |= (byte)(unpackedQuints[1] << bits);
                    results[3 * i + 2] |= (byte)(unpackedQuints[2] << bits);
                }
            }

            return results;
        }

        public static void Encode(BitWriter bw, int quantizationLevel, byte[] values)
        {
            var lowParts = new byte[64];
            var highParts = new byte[69];      // 64 elements + 5 elements for padding
            var tqBlocks = new byte[22];      // trit-blocks or quint-blocks

            var (bits, trits, quints) = GetNumberOfUnits(quantizationLevel);

            for (var i = 0; i < values.Length; i++)
            {
                lowParts[i] = (byte)(values[i] & ((1 << bits) - 1));
                highParts[i] = (byte)(values[i] >> bits);
            }

            // construct trit-blocks or quint-blocks as necessary
            if (trits > 0)
            {
                var tritBlocks = (values.Length + 4) / 5;
                for (var i = 0; i < tritBlocks; i++)
                    tqBlocks[i] = _integerOfTrit[highParts[5 * i + 4]][highParts[5 * i + 3]][highParts[5 * i + 2]][highParts[5 * i + 1]][highParts[5 * i]];
            }

            if (quints > 0)
            {
                var quintBlocks = (values.Length + 2) / 3;
                for (var i = 0; i < quintBlocks; i++)
                    tqBlocks[i] = _integerOfQuint[highParts[3 * i + 2]][highParts[3 * i + 1]][highParts[3 * i]];
            }

            // then, write out the actual bits.
            var lCounter = 0;
            var hCounter = 0;
            for (var i = 0; i < values.Length; i++)
            {
                bw.WriteBits(lowParts[i], bits);

                if (trits > 0)
                {
                    var tData = tqBlocks[hCounter] >> tritsBlockShift[lCounter];
                    bw.WriteBits(tData, tritsBits[lCounter]);
                    hCounter += tritsHCounterIncrement[lCounter];
                    lCounter = tritsNextLCounter[lCounter];
                }

                if (quints > 0)
                {
                    var tData = tqBlocks[hCounter] >> quintsBlockShift[lCounter];
                    bw.WriteBits(tData, quintsBits[lCounter]);
                    hCounter += quintsHCounterIncrement[lCounter];
                    lCounter = quintsNextLCounter[lCounter];
                }
            }
        }

        public static (int bits, int trits, int quints) GetNumberOfUnits(int quantizationLevel)
        {
            switch (quantizationLevel)
            {
                case 0:
                    return (1, 0, 0);
                case 1:
                    return (0, 1, 0);

                case 2:
                    return (2, 0, 0);
                case 3:
                    return (0, 0, 1);
                case 4:
                    return (1, 1, 0);

                case 5:
                    return (3, 0, 0);
                case 6:
                    return (1, 0, 1);
                case 7:
                    return (2, 1, 0);

                case 8:
                    return (4, 0, 0);
                case 9:
                    return (2, 0, 1);
                case 10:
                    return (3, 1, 0);

                case 11:
                    return (5, 0, 0);
                case 12:
                    return (3, 0, 1);
                case 13:
                    return (4, 1, 0);

                case 14:
                    return (6, 0, 0);
                case 15:
                    return (4, 0, 1);
                case 16:
                    return (5, 1, 0);

                case 17:
                    return (7, 0, 0);
                case 18:
                    return (5, 0, 1);
                case 19:
                    return (6, 1, 0);

                case 20:
                    return (8, 0, 0);
            }

            return default;
        }

        public static int ComputeBitCount(int items, int quantizationLevel)
        {
            switch (quantizationLevel)
            {
                case 0:
                    return items;
                case 1:
                    return (8 * items + 4) / 5;

                case 2:
                    return 2 * items;
                case 3:
                    return (7 * items + 2) / 3;
                case 4:
                    return (13 * items + 4) / 5;

                case 5:
                    return 3 * items;
                case 6:
                    return (10 * items + 2) / 3;
                case 7:
                    return (18 * items + 4) / 5;

                case 8:
                    return items * 4;
                case 9:
                    return (13 * items + 2) / 3;
                case 10:
                    return (23 * items + 4) / 5;

                case 11:
                    return 5 * items;
                case 12:
                    return (16 * items + 2) / 3;
                case 13:
                    return (28 * items + 4) / 5;

                case 14:
                    return 6 * items;
                case 15:
                    return (19 * items + 2) / 3;
                case 16:
                    return (33 * items + 4) / 5;

                case 17:
                    return 7 * items;
                case 18:
                    return (22 * items + 2) / 3;
                case 19:
                    return (38 * items + 4) / 5;

                case 20:
                    return 8 * items;
            }

            return default;
        }
    }
}
