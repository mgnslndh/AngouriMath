﻿module AngouriMath.Terminal.Lib.PreRunCode

open AngouriMath.Terminal.Lib.FSharpInteractive

let OpensAndOperators = @"
open AngouriMath
open AngouriMath.FSharp.Core
open AngouriMath.FSharp.Functions
open AngouriMath.FSharp.Matrices
open AngouriMath.FSharp.Shortcuts
open AngouriMath.FSharp.Constants

let eval (x : obj) = 
    match (parsed x).InnerSimplified with
    | :? Entity.Number.Integer as i -> i.ToString()
    | :? Entity.Number.Rational as i -> i.RealPart.EDecimal.ToString()
    | :? Entity.Number.Real as re -> re.RealPart.EDecimal.ToString()
    | :? Entity.Number.Complex as cx -> cx.RealPart.EDecimal.ToString() + "" + "" + cx.ImaginaryPart.EDecimal.ToString() + ""i""
    | other -> (evaled other).ToString()

open AngouriMath.Interactive.AggressiveOperators

let x = symbol ""x""
let y = symbol ""y""
let a = symbol ""a""
let b = symbol ""b""
"

let enableAngouriMath kernel =
    let innerCode = OpensAndOperators.Replace("\"", "\\\"")
    let preRunCode = OpensAndOperators + $"let preRunCode = \"{innerCode}\""
    execute kernel preRunCode
