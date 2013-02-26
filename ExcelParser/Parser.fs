﻿module ExcelParser
    open FParsec
    open AST

    type UserState = unit
    type Parser<'t> = Parser<'t, UserState>

    // In the parser definitions below, 'ws' is an Interop reference to the CURRENT worksheet
    let fake_ws: Worksheet = null

    // Addresses
    let AddrR: Parser<_> = pstring "R" >>. pint32
    let AddrC: Parser<_> = pstring "C" >>. pint32
    let AddrR1C1 ws: Parser<_> = pipe2 AddrR AddrC (fun r c -> Address(r,c,ws))

    // Ranges
    let MoreAddr ws: Parser<_> = pstring ":" >>. (AddrR1C1 ws)
    let RangeR1C1 ws: Parser<_> = pipe2 (AddrR1C1 ws) (MoreAddr ws) (fun r1 r2 -> Range(r1, r2))

    // Worksheet Names
    let WorksheetNameQuoted: Parser<_> = between (pstring "'") (pstring "'") (many1Satisfy ((<>) '\''))
    let WorksheetNameUnquoted: Parser<_> = many1Satisfy (fun c -> (isDigit c) || (isLetter c))
    let WorksheetName: Parser<_> = WorksheetNameQuoted <|> WorksheetNameUnquoted

    // Workbook Names (this may be too restrictive)
    let WorkbookName: Parser<_> = between (pstring "[") (pstring "]") (many1Satisfy (fun c -> c <> '[' && c <> ']'))
    let Workbook: Parser<_> = (attempt WorkbookName) <|> (pstring "")

    // References
    // References consist of the following parts:
    //   An optional worksheet name prefix
    //   A single-cell ("Address") or multi-cell address ("Range")
    let RangeReferenceWorksheet ws: Parser<_> = pipe2 (WorksheetName .>> pstring "!") (RangeR1C1 ws) (fun wsname rng -> ReferenceRange(Some(wsname), rng) :> Reference)
    let RangeReferenceNoWorksheet ws: Parser<_> = (RangeR1C1 ws) |>> (fun rng -> ReferenceRange(None, rng) :> Reference)
    let RangeReference ws: Parser<_> = (attempt (RangeReferenceWorksheet ws)) <|> (RangeReferenceNoWorksheet ws)
    let AddressReferenceWorksheet ws: Parser<_> = pipe2 (WorksheetName .>> pstring "!") (AddrR1C1 ws) (fun wsname addr -> ReferenceAddress(Some(wsname), addr) :> Reference)
    let AddressReferenceNoWorksheet ws: Parser<_> = (AddrR1C1 ws) |>> (fun addr -> ReferenceAddress(None, addr) :> Reference)
    let AddressReference ws: Parser<_> = (attempt (AddressReferenceWorksheet ws)) <|> (AddressReferenceNoWorksheet ws)
    let ReferenceAorR ws: Parser<_> = (attempt (RangeReference ws)) <|> (AddressReference ws)
    let Reference ws: Parser<_> = pipe2 Workbook (ReferenceAorR ws) (fun wbname ref -> ref.WorkbookName <- Some wbname; ref)
    let ReferenceList ws: Parser<_> = sepBy1 (Reference ws) (pstring ",")

    // Expressions
    let Expression ws: Parser<_> = ReferenceList ws

    // Formulas
    let Formula ws: Parser<_> = pstring "=" >>. (Expression ws) .>> eof

    // monadic wrapper for success/failure
    let test p str =
        match run p str with
        | Success(result, _, _)   -> printfn "Success: %A" result
        | Failure(errorMsg, _, _) -> printfn "Failure: %s" errorMsg

    let GetAddress str ws: Address =
        match run ((AddrR1C1 ws) .>> eof) str with
        | Success(addr, _, _) -> addr
        | Failure(errorMsg, _, _) -> failwith errorMsg

    let GetRange str ws: Range option =
        match run ((RangeR1C1 ws) .>> eof) str with
        | Success(range, _, _) -> Some(range)
        | Failure(errorMsg, _, _) -> None

    let GetReference str ws: Reference option =
        match run ((Reference ws) .>> eof) str with
        | Success(reference, _, _) -> Some(reference)
        | Failure(errorMsg, _, _) -> None

    // helper function for mortals to comprehend; note that Formula looks for EOF
    let ConsoleTest(s: string) = test (Formula fake_ws) s