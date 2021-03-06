﻿using System;
using System.IO;

namespace Doorstop
{
    public static class Loader
    {
        public static void Main(string[] args)
        {
            using (TextWriter tw = File.CreateText("doorstop_is_alive.txt"))
            {
                tw.WriteLine($"Hello! This text file was generated by Doorstop on {DateTime.Now:R}!");
                tw.WriteLine($"I was called with {args.Length} params!");

                for (int i = 0; i < args.Length; i++)
                    tw.WriteLine($"{i} => {args[i]}");

                tw.WriteLine("Replace this DLL with a custom-made one to do whatever you want!");

                tw.Flush();
            }
        }
    }
}