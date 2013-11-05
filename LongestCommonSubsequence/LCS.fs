﻿module LongestCommonSubsequence
    type Sign =
    | Plus = 0
    | Minus = 1
    | Empty = 2

    type Error = 
    // first int is index in orig string; second int is delta
    | TranspositionError of int*int
    // first int is index in orig string; char is the character we we supposed to type;
    // string is what was actually typed
    | TypoError of int*char*string
    // first int is index in orig string; second is Some(new index) or None if decimal was dropped
    | DecimalError of int*int option
    // first sign is orig string; second is in retyped string
    | SignError of Sign*Sign

    // compute all of the LCS lengths
    let LCSLength(X: string, Y: string) : int[,] =
        let m = X.Length
        let n = Y.Length
        let C = Array2D.create (m + 1) (n + 1) 0
        for i = 1 to m do
            for j = 1 to n do
                if X.[i-1] = Y.[j-1] then
                    C.[i,j] <- C.[i-1,j-1] + 1
                else
                    C.[i,j] <- System.Math.Max(C.[i,j-1],C.[i-1,j])
        C

    // this function reads out all of the longest subsequences
    let rec backtrackAll(C: int[,], X: string, Y: string, i: int, j: int) : Set<string> =
        if i = 0 || j = 0 then
            set[""]
        else if X.[i-1] = Y.[j-1] then
            Set.map (fun (Z: string) -> Z + X.[i-1].ToString()) (backtrackAll(C, X, Y, i-1, j-1))
        else
            let mutable R = Set.empty
            if C.[i,j-1] >= C.[i-1,j] then
                R <- backtrackAll(C, X, Y, i, j-1)
            if C.[i-1,j] >= C.[i,j-1] then
                R <- Set.union R (backtrackAll(C, X, Y, i-1, j))
            R

    // like backtrackAll except that it returns a set of character pair
    // sequences instead of a set of strings
    // for each character pair: (X pos, Y pos)
    let rec getCharPairs(C: int[,], X: string, Y: string, i: int, j: int) : Set<(int*int) list> =
        if i = 0 || j = 0 then
            set[[]]
        else if X.[i-1] = Y.[j-1] then
            let mutable ZS = Set.map (fun (Z: (int*int) list) -> Z @ [(i-1,j-1)] ) (getCharPairs(C, X, Y, i-1, j-1))
            if (C.[i,j] = C.[i,j-1]) then 
                ZS <- Set.union ZS (getCharPairs(C, X, Y, i, j-1))
            ZS
        else
            let mutable R = Set.empty
            if C.[i,j-1] >= C.[i-1,j] then
                R <- getCharPairs(C, X, Y, i, j-1)
            if C.[i-1,j] >= C.[i,j-1] then
                R <- Set.union R (getCharPairs(C, X, Y, i-1, j))
            R

    // compute the set of longest strings
    let LCS(X: string, Y: string) : Set<string> =
        let m = X.Length
        let n = Y.Length
        let C = LCSLength(X,Y)
        backtrackAll(C, X, Y, m, n)

    // compute the set of longest character pair sequences
    let LCS_Char(X: string, Y: string) : Set<(int*int) list> =
        let m = X.Length
        let n = Y.Length
        let C = LCSLength(X,Y)
        getCharPairs(C, X, Y, m, n)

    // "pull" each Y index to the left as far as it will go
    // this allows key-repeat typos to be counted correctly
    let LeftAlignedLCS(orig: string, entered: string) : (int*int) list =
        let charseqs = LCS_Char(orig, entered) |> Set.toArray
        // randomly choose one of the longest subsequences
        let rng = System.Random()
        let charseq = charseqs.[rng.Next(charseqs.Length - 1)]
        // new sequence
        let mutable newseq: (int*int) list = []
        // realign characters
        let mutable jstop = 0
        for (i,j) in charseq do
            // all of the candidates to the left of j, but only as far as jstop
            let candidate_indices = List.rev [jstop..j]
            let mutable keep_looking = true
            for k in candidate_indices do
                if keep_looking then
                    if entered.[k] = entered.[j] then
                        jstop <- k
                    else
                        keep_looking <- false
            newseq <- newseq @ [(i,jstop)]
        // return realigned char list
        newseq

    // find each missing character in original, by index into original string
    let GetMissingCharIndices(orig: string, align: (int*int) list) : int list =
        let all_indices = set[0..orig.Length - 1]
        let align_indices = List.map (fun (o,_) -> o) align |> Set.ofList
        Set.difference all_indices align_indices |> Set.toList

    // find each appended character in entered, by index into entered string
    let GetAddedCharIndices(entered: string, align: (int*int) list) : int list =
        let all_indices = set[0..entered.Length - 1]
        let align_indices = List.map (fun (_,e) -> e) align |> Set.ofList
        Set.difference all_indices align_indices |> Set.toList

    // this is for C# unit test use
    let LeftAlignedLCSList(orig: string, entered: string) : System.Collections.Generic.IEnumerable<(int*int)> =
        LeftAlignedLCS(orig, entered) |> List.toSeq

    let LCS_Hash(X: string, Y: string) : System.Collections.Generic.HashSet<string> =
        let hs = new System.Collections.Generic.HashSet<string>()
        for s in LCS(X,Y) do hs.Add(s) |> ignore
        hs

    let LCS_Hash_Char(X: string, Y: string) : System.Collections.Generic.HashSet<System.Collections.Generic.IEnumerable<int*int>> =
        let hs = new System.Collections.Generic.HashSet<System.Collections.Generic.IEnumerable<int*int>>()
        for ls in LCS_Char(X,Y) do hs.Add(ls |> List.toSeq) |> ignore
        hs