﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.ServiceProcess;


namespace BaseWindowsService
{

    public abstract partial class BaseWindowsService<T> : ServiceBase where T : BaseWindowsService.BaseWindowsService<T>, new()
    {
        private bool stopping;
        private ManualResetEvent stoppedEvent = new ManualResetEvent(false);

        public BaseWindowsService()
        {
            InitializeComponent();
            this.ServiceName = this.GetType().Name;
        }

        public void Go(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                // if no arguments exist run service as normal
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[] 
                { 
                    new T() 
                };
                ServiceBase.Run(ServicesToRun);
            }
            else if (args[0].Equals("debug", StringComparison.CurrentCultureIgnoreCase))
            {
                // if debug arguments exist run as a console app (debugging)
                T s = new T();
                s.StartService(args);

                Console.WriteLine("Press enter to exit");
                Console.ReadLine();

                s.StopService();
                Environment.Exit(0);
            }
            else if (args[0].Equals("i", StringComparison.CurrentCultureIgnoreCase))
            {
                //install
            }
            else if (args[0].Equals("u", StringComparison.CurrentCultureIgnoreCase))
            {
                //uninstall
            }
        }

        public void StartService(string[] args)
        {
            Logger.Log(string.Format("Starting {0} work thread.", this.ServiceName), TraceEventType.Information);
            ThreadPool.QueueUserWorkItem(delegate
            {
                DoWork(args);
            }, null);
        }

        public abstract void DoYourMagic(string[] args);

        public void StopService()
        {
            int wait = Convert.ToInt32(ConfigurationManager.AppSettings["StopServiceWait"]);
            Logger.Log(string.Format("Requesting {0} work thread stop.", this.ServiceName), TraceEventType.Information);
            this.stopping = true;

            if (!this.stoppedEvent.WaitOne(wait))
            {
                Logger.Log(string.Format("Requested {0} work thread stop timed out.", this.ServiceName), TraceEventType.Information);
            }
        }

        protected override void OnStart(string[] args)
        {
            StartService(args);
        }

        protected override void OnStop()
        {
            StopService();
        }

        private void DoWork(string[] args)
        {
            int wait = Convert.ToInt32(ConfigurationManager.AppSettings["ActionSleep"]);

            while (!this.stopping)
            {
                DoYourMagic(args);
                Thread.Sleep(wait);
            }

            Logger.Log(string.Format("Stopping {0} work thread.", this.ServiceName), TraceEventType.Information);
            this.stoppedEvent.Set();
        }
    }
}
