﻿using System;
using Discord.Commands;

namespace CSharpDewott
{
    class Globals
    {
        public static readonly Random Random = new Random();

        public static CommandService CommandService;

        public static byte[] EncryptKey { get; set; }

        //public static List<IDisposable> TypingDisposable = new List<IDisposable>();

    }
}