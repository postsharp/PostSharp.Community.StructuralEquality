﻿using System;

 namespace PostSharp.Community.StructuralEquality.Tests.Fody.AssemblyToProcess.Enums
 {
     [Flags]
     public enum FlagsEnum
     {
         G = 0,
         H = 1,
         I = 2,
         J = 4,
         K = 8,
     }
 }