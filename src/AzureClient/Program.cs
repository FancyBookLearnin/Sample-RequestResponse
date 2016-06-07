using System.Runtime.Remoting.Messaging;
using MassTransit.AzureServiceBusTransport;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Configuration;

namespace Client
{
    using System;
    using System.Configuration;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using log4net.Config;
    using MassTransit;
    using MassTransit.Log4NetIntegration.Logging;
    using Sample.MessageTypes;


    class Program
    {
        static void Main()
        {
            ConfigureLogger();

            // MassTransit to use Log4Net
            Log4NetLogger.Use();

            IBusControl busControl = CreateBus();

            try
            {
                busControl.Start();

                for (;;)
                {
                    Console.Write("Enter customer id (quit exits): ");
                    string customerId = Console.ReadLine();
                    if (customerId == "quit")
                        break;

                    // this is run as a Task to avoid weird console application issues
                    Task.Run(async () =>
                    {
                        var request = new SimpleRequest(customerId);

                        var sep =
                            busControl.GetSendEndpoint(
                                    new Uri(ConfigurationManager.AppSettings["QueueUri"])).Result;

                        await sep.Send<SimpleRequest>(request);
                    }).Wait();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception!!! OMG!!! {0}", ex);
            }
            finally
            {
                busControl.Stop();
            }

            Console.WriteLine("---end---");
            Console.ReadLine();
        }


        static IBusControl CreateBus()
        {
            return Bus.Factory.CreateUsingAzureServiceBus(cfg =>
            {
                cfg.Host(new Uri(ConfigurationManager.AppSettings["EndPoint"]),
                    h =>
                    {
                        h.TokenProvider =
                            TokenProvider.CreateSharedAccessSignatureTokenProvider(
                            ConfigurationManager.AppSettings["SharedAccessKeyName"],
                            ConfigurationManager.AppSettings["SharedAccessKey"]);
                    });
                cfg.OverrideDefaultBusEndpointQueueName(ConfigurationManager.AppSettings["Queue"]);
            });
        }

        static void ConfigureLogger()
        {
            const string logConfig = @"<?xml version=""1.0"" encoding=""utf-8"" ?>
<log4net>
  <root>
    <level value=""INFO"" />
    <appender-ref ref=""console"" />
  </root>
  <appender name=""console"" type=""log4net.Appender.ColoredConsoleAppender"">
    <layout type=""log4net.Layout.PatternLayout"">
      <conversionPattern value=""%m%n"" />
    </layout>
  </appender>
</log4net>";

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(logConfig)))
            {
                XmlConfigurator.Configure(stream);
            }
        }


        class SimpleRequest :
            ISimpleRequest
        {
            readonly string _customerId;
            readonly DateTime _timestamp;

            public SimpleRequest(string customerId)
            {
                _customerId = customerId;
                _timestamp = DateTime.UtcNow;
            }

            public DateTime Timestamp
            {
                get { return _timestamp; }
            }

            public string CustomerId
            {
                get { return _customerId; }
            }
        }
    }
}