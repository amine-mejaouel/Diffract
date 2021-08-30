﻿module Diffract.DiffPrinter

open System.IO

let toStreamImpl param (w: TextWriter) (d: Diff) =
    let rec loop (indent: string) (path: string) (d: Diff) =
        match d with
        | Diff.Value (x1, x2) ->
            w.WriteLine($"%s{indent}%s{param.x1Name}%s{path} = %A{x1}")
            w.WriteLine($"%s{indent}%s{param.x2Name}%s{path} = %A{x2}")
        | Diff.Record [field] ->
            loop indent $"%s{path}.%s{field.Name}" field.Diff
        | Diff.Record fields ->
            w.WriteLine($"%s{indent}%s{param.neutralName}%s{path} differs by %i{List.length fields} fields:")
            let indent = indent + param.indent
            for field in fields do
                loop indent $"%s{path}.%s{field.Name}" field.Diff
        | Diff.UnionCase (caseName1, caseName2) ->
            w.WriteLine($"%s{indent}%s{param.neutralName}%s{path} differs by union case:")
            let indent = indent + param.indent
            w.WriteLine($"%s{indent}%s{param.x1Name}%s{path} is %s{caseName1}")
            w.WriteLine($"%s{indent}%s{param.x2Name}%s{path} is %s{caseName2}")
        | Diff.UnionField (_case, [field]) ->
            loop indent $"%s{path}.%s{field.Name}" field.Diff
        | Diff.UnionField (case, fields) ->
            w.WriteLine($"%s{indent}%s{param.neutralName}%s{path} differs by union case %s{case} fields:")
            let indent = indent + param.indent
            for field in fields do
                loop indent $"%s{path}.%s{field.Name}" field.Diff
        | Diff.CollectionCount (c1, c2) ->
            w.WriteLine($"%s{indent}%s{param.neutralName}%s{path} collection differs by count:")
            w.WriteLine($"%s{indent}%s{param.indent}%s{param.x1Name}%s{path}.Count = %i{c1}")
            w.WriteLine($"%s{indent}%s{param.indent}%s{param.x2Name}%s{path}.Count = %i{c2}")
        | Diff.CollectionContent diffs ->
            w.WriteLine($"%s{indent}%s{param.neutralName}%s{path} collection differs by content:")
            let indent = indent + param.indent
            for item in diffs do
                loop indent $"%s{path}[%s{item.Name}]" item.Diff
        | Diff.Custom cd ->
            cd.WriteTo(w, param, indent, path, loop)
        | Diff.Dictionary (keysInX1, keysInX2, common) ->
            w.WriteLine($"%s{indent}%s{param.neutralName}%s{path} dictionary differs:")
            for k in keysInX1 do
                w.WriteLine($"%s{indent}%s{param.x2Name}%s{path}[%s{k}] is missing")
            for k in keysInX2 do
                w.WriteLine($"%s{indent}%s{param.x1Name}%s{path}[%s{k}] is missing")
            for item in common do
                loop indent $"%s{path}[%s{item.Name}]" item.Diff
    loop "" "" d

let write (param: PrintParams) (w: TextWriter) (d: Diff option) =
    match d with
    | None -> w.WriteLine($"No differences between {param.x1Name} and {param.x2Name}.")
    | Some d -> toStreamImpl param w d

let toString param d =
    use w = new StringWriter()
    write param w d
    w.ToString()
