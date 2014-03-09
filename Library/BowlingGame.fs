module BowlingGame

type Score =
    | Strike
    | Spare
    | Try of int
    | Miss

let score tries =
    let decide x =
        match x with
        | Miss -> 123
    0