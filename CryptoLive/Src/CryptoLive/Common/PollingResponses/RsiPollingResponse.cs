﻿using System;
using Common.Abstractions;

namespace Common.PollingResponses
{
    public class RsiPollingResponse : PollingResponseBase
    {
        public RsiPollingResponse(DateTime time,
            decimal rsi,
            bool isCancelled=false,
            Exception gotException=null)
            : base(time, isCancelled, gotException)
        {
            Rsi = rsi;
        }

        public decimal Rsi { get; }

        public override string ToString()
        {
            return $"Rsi: {Rsi:F2}";
        }
    }
}