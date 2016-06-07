namespace AzureRequestService
{
    using System;
    using System.Configuration;
    using MassTransit;
    using MassTransit.AzureServiceBusTransport;
    using Topshelf;
    using Topshelf.Logging;
    using Microsoft.ServiceBus;

    class RequestService :
        ServiceControl
    {
        readonly LogWriter _log = HostLogger.Get<RequestService>();

        IBusControl _busControl;

        public bool Start(HostControl hostControl)
        {
            _log.Info("Creating bus...");
            _busControl = Bus.Factory.CreateUsingAzureServiceBus(cfg =>
            {
                cfg.Host(new Uri(ConfigurationManager.AppSettings["EndPoint"]),
                    h =>
                    {
                        h.TokenProvider =
                             TokenProvider.CreateSharedAccessSignatureTokenProvider(
                                 ConfigurationManager.AppSettings["SharedAccessKeyName"],
                                 ConfigurationManager.AppSettings["SharedAccessKey"]);
                    });
                
                cfg.ReceiveEndpoint("moviemagic", e => { e.Consumer<RequestConsumer>(); });
            });
           

            _log.Info("Starting bus...");

            _busControl.Start();           

            return true;
        }

        public bool Stop(HostControl hostControl)
        {
            _log.Info("Stopping bus...");

            _busControl?.Stop();

            return true;
        }
    }
}