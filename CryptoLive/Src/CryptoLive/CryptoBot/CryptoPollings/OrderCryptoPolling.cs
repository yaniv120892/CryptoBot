using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Abstractions;
using Common.PollingResponses;
using CryptoBot.Abstractions;
using Services.Abstractions;
using Utils.Abstractions;

namespace CryptoBot.CryptoPollings
{
    public class OrderCryptoPolling : CryptoPollingBase
    {
        private static string s_actionName = "Order polling";
        private static readonly int s_maxIterations = 5;
        private static readonly int s_delayTimeInSeconds = 60;

        private readonly ISystemClock m_systemClock;
        private readonly ITradeService m_tradeService;
        private readonly long m_orderId;
        
        public OrderCryptoPolling(ISystemClock systemClock,
            ITradeService tradeService,
            long orderId)
        {
            m_orderId = orderId;
            m_systemClock = systemClock;
            m_tradeService = tradeService;
            PollingType = nameof(OrderCryptoPolling);
        }

        protected override async Task<PollingResponseBase> StartAsyncImpl(CancellationToken cancellationToken)
        {
            int counter = 0;
            string orderStatus = await m_tradeService.GetOrderStatusAsync(Currency, m_orderId, CurrentTime);
            while (!orderStatus.Equals("Filled") && counter < s_maxIterations)
            {
                CurrentTime = await m_systemClock.Wait(cancellationToken, Currency, s_delayTimeInSeconds,
                    s_actionName, CurrentTime);
                counter++;
                orderStatus = await m_tradeService.GetOrderStatusAsync(Currency, m_orderId, CurrentTime);
            }
            
            Console.WriteLine(counter);
            if (orderStatus.Equals("Filled"))
            {
                var orderPollingResponse = new OrderPollingResponse(CurrentTime, m_orderId);
                return orderPollingResponse;
            }
            
            throw new OperationCanceledException();
        }
        
        protected override string StartPollingDescription() =>
            $"{PollingType} {Currency} {CurrentTime:dd/MM/yyyy HH:mm:ss} start, " +
            $"Get update every {s_delayTimeInSeconds / 60} minutes, max iterations {s_maxIterations}"; 
        
        protected override PollingResponseBase CreateExceptionPollingResponse(Exception e) => 
            new OrderPollingResponse(CurrentTime, m_orderId, false, e);

        protected override PollingResponseBase CreateGotCancelledPollingResponse() => 
            new OrderPollingResponse(CurrentTime, m_orderId, true);
    }
}