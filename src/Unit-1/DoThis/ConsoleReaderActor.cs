using System;
using Akka.Actor;

namespace WinTail
{
    /// <summary>
    /// Actor responsible for reading FROM the console. 
    /// Also responsible for calling <see cref="ActorSystem.shutdown"/>.
    /// </summary>
    class ConsoleReaderActor : UntypedActor
    {
        // note: we don't even need our own constructor anymore!
        public const string StartCommand = "start";
        public const string ExitCommand = "exit";

        protected override void OnReceive(object message)
        {
            if (message.Equals(StartCommand))
            {
                DoPrintInstructions();
            }
            
            GetAndValidateInput();
        }

        #region Internal methods
        private void DoPrintInstructions()
        {
            Console.WriteLine("Please provide the URI of a log file on disk.\n");
        }
        
        /// <summary>
        /// Reads input form console, validate it, then signals appropriate response
        /// (continue processing, error, success, etc.)
        /// </summary>
        private void GetAndValidateInput()
        {
            var message = Console.ReadLine();

            if (!string.IsNullOrEmpty(message) &&
                String.Equals(message, ExitCommand, StringComparison.OrdinalIgnoreCase))
            {
                // if user typed ExitCommand, shut down the entire actor
                // system (allows the process to exit)
                Context.System.Terminate();
                return;
            }

            //otherwise, just hand message off for validation
            Context.ActorSelection("akka://MyActorSystem/user/validatorActor").Tell(message);
        }
        #endregion
    }
}