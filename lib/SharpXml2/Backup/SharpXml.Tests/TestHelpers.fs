//  Copyright 2012-2014 Gregor Uhlenheuer
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

namespace SharpXml.Tests

module TestHelpers =

    open System.Collections.Generic
    open System.Diagnostics
    open System.Linq

    open NUnit.Framework
    open NUnit.Framework.Constraints

    let time func iterations =
        let sw = Stopwatch.StartNew()
        let rec loop f i =
            if i > 0 then f(); loop f (i-1)
        loop func iterations
        sw.Stop()
#if DEBUG
        Debug.WriteLine <| sprintf "Iterations: %d; Elapsed: %A" iterations sw.Elapsed
#else
        System.Console.WriteLine("Iterations: {0}; Elapsed: {1}", iterations, sw.Elapsed)
#endif

    let timeAvg func iterations =
        let times = List<int64>()
        let rec loop f i =
            if i > 0 then
                let sw = Stopwatch.StartNew()
                f()
                sw.Stop()
                times.Add(sw.ElapsedMilliseconds)
                loop f (i-1)
        loop func iterations
        let avg = times.Average()
#if DEBUG
        Debug.WriteLine <| sprintf "Iterations: %d; Average: %.0f" iterations avg
#else
        System.Console.WriteLine("Iterations: {0}; Average: {1}", iterations, avg)
#endif

    let should (func : 'a -> #Constraint) x (actual : obj) =
        let constr = func x
        Assert.That(actual, constr)

    let shouldBe (func : #Constraint) (actual : obj) =
        Assert.That(actual, func)

    let equal x = EqualConstraint(x)

    let contain x = ContainsConstraint(x)

    let sameAs x = SameAsConstraint(x)

    let throw x = ThrowsConstraint(x)

    let throwNothing = ThrowsNothingConstraint()

    let Null = NullConstraint()

    let notNull = NotConstraint(NullConstraint())

    let True = TrueConstraint()

    let False = FalseConstraint()