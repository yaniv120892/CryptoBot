using System;
using Common;

namespace Utils.Converters
{
    public class BotResultConverter
    {
        public static BotResult ConvertIntToBotResult(int value)
        {
            switch (value)
            {
                case 1:
                    return BotResult.Gain;
                case 0:
                    return BotResult.Even;
                case -1:
                    return BotResult.Loss;
            }

            throw new Exception($"Failed to convert int to BotResult, value should be -1,0,1 but was {value}");
        }
    }
}