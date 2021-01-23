﻿using System;
using Akka.Actor;

namespace WinTail
{
    #region Program
    class Program
    {
        public static ActorSystem MyActorSystem;

        static void Main(string[] args)
        {
            // initialize MyActorSystem
            MyActorSystem = ActorSystem.Create("MyActorSystem");

            // nothing here where our actors used to be!

            Props consoleWriterProps = Props.Create<ConsoleWriterActor>();
            IActorRef consoleWriterActor = MyActorSystem.ActorOf(consoleWriterProps, "consoleWriterActor");
            
            // make tailCoordinatorActor
            Props tailCoordinatorProps = Props.Create<TailCoordinatorActor>();
            IActorRef tailCoordinatorActor = MyActorSystem.ActorOf(tailCoordinatorProps, "tailCoordinatorActor");

            // pass tailCoordinatorActor to fileValidatiorActorProps (just adding one extra arg)
            Props fileValidatorActorProps = Props.Create<FileValidatorActor>(consoleWriterActor);
            IActorRef fileValidatorActor = MyActorSystem.ActorOf(fileValidatorActorProps, "validatorActor");

            Props consoleReaderProps = Props.Create<ConsoleReaderActor>();
            IActorRef consoleReaderActor = MyActorSystem.ActorOf(consoleReaderProps, "consoleReaderActor");

            // tell console reader to begin
            consoleReaderActor.Tell(ConsoleReaderActor.StartCommand);

            // blocks the main thread from exiting until the actor system is shut down
            MyActorSystem.WhenTerminated.Wait();
        }
    }
    #endregion
}
