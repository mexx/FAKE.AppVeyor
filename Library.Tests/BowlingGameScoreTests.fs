module ``A game of bowling``

open Xunit
open FsUnit.Xunit

[<Fact>]
let ``with all miss should get the expected score`` () =
    BowlingGame.score "--------------------" |> should equal 0