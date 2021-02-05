﻿using System;
using Common.Abstractions;

namespace Common.PollingResponses
{
    public class RsiPollingResponse : IPollingResponse
    {
        public RsiPollingResponse(DateTime time, decimal rsi)
        {
            Time = time;
            Rsi = rsi;
        }

        public DateTime Time { get; }
        public decimal Rsi { get; }

        public override string ToString()
        {
            return $"Rsi: {Rsi}, Time: {Time}";
        }
    }
}