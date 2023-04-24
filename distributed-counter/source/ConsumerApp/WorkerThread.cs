using CosmosDistributedCounter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DistributedCounterConsumerApp
{
    public record ConsoleMessage(
           string Message,
           ConsoleColor Color
       );

    // Delegate that defines the signature for the callback method.
    public delegate void PostMessageCallback(ConsoleMessage msg);

    internal class WorkerThread
    {
        PrimaryCounter pc;
        DistributedCounterOperationalService dcos;
        public bool isActive = true;
        PostMessageCallback postMessage;
        public WorkerThread(PrimaryCounter _pc, DistributedCounterOperationalService _dcos, PostMessageCallback _postMessage)
        {
            this.pc = _pc;
            this.dcos = _dcos;
            this.postMessage = _postMessage;
        }


        public async void StartThread()
        {            
            while (this.isActive)
            {
                //pick a random number to decrement
                Random r = new Random();
                int decrementVal = r.Next(1, 4);

                try
                {
                    if (await dcos.DecrementDistributedCounterValueAsync(pc, decrementVal) == false)
                    {
                        postMessage(new ConsoleMessage($"Warning: Decrement Failed", ConsoleColor.Yellow));
                    }
                    else
                    {
                        postMessage(new ConsoleMessage($"Sucess: Decrement by "+ decrementVal, ConsoleColor.Blue));
                    }
                }
                catch (Exception ex)
                {
                    postMessage(new ConsoleMessage($"Exception: " + ex.Message, ConsoleColor.Red));
                }

                //DO WORK, delay before next execution
                await DoWork();
            }
        }

        private async Task DoWork()
        {
            //wait some random time
            Random r = new Random();
            int delay = r.Next(250, 500);
           
            await Task.Delay(delay);
        }
    }
}
