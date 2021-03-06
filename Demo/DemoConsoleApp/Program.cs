﻿using System;
using System.Collections.Generic;
using RCNet.DemoConsoleApp.Log;

namespace RCNet.DemoConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            ////Research - this is not a part of the demo - it is a free playground
            //(new Research()).Run();

            //Demo standard execution
            try
            {
                //Run the demo
                SMDemo.RunDemo(new ConsoleLog(), @"DemoSettings.xml");
            }
            catch(Exception e)
            {
                Console.WriteLine();
                while(e != null)
                {
                    Console.WriteLine(e.Message);
                    e = e.InnerException;
                }
            }
            Console.WriteLine("Press Enter.");
            Console.ReadLine();
            return;
        }//Main

    }//Program

}//Namespace
