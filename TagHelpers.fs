module MvcMovieGiraffe.TagHelpers

open System.Linq

open System.ComponentModel.DataAnnotations

open Giraffe.ViewEngine

// ----------------------------------------------------------------------

open FSharp.Quotations
open FSharp.Quotations.Patterns

let (|PropInfo|_|) (e : Expr<'a>) =
    match e with
    | Patterns.PropertyGet (obj_instance, prop_info, _body_expressions) ->
        let getter =
            match obj_instance with
            | None                                -> fun () -> prop_info.GetValue(null)
            | Some (ValueWithName(v, _ty, _name)) -> fun () -> prop_info.GetValue(v)
            | _                                   -> fun () -> box null
        
        Some(prop_info, getter)

    | _ -> None

// ----------------------------------------------------------------------

[<RequireQualifiedAccess>]
type Input =
    static member Of([<ReflectedDefinition>] expr: Expr<'a>, attrs_a: XmlAttribute list) =
        match expr with
        | PropInfo(property_info, get_current_value) ->
                                                            
            let display_name =

                let cattr = System.Attribute.GetCustomAttribute(property_info, typedefof<DisplayAttribute>) :?> DisplayAttribute

                if (not (isNull cattr)) then
                    cattr.Name
                else
                    property_info.Name

            // System.Console.WriteLine(property_name + " : " + property_info.PropertyType.Name)
            
            let type_attribute_provided = attrs_a.Any(fun xml_attr ->
                match xml_attr with
                | KeyValue (attr_key, attr_val) -> attr_key = "type"
                | Boolean str -> false)

            let type_value =

                let data_type_attr = System.Attribute.GetCustomAttribute(property_info, typedefof<DataTypeAttribute>) :?> DataTypeAttribute

                if not type_attribute_provided then
                    if (not (isNull data_type_attr)) && (data_type_attr.DataType = DataType.Date) then
                        "date"
                    else
                        match property_info.PropertyType.Name with
                        | "Int64"    -> "number"
                        | "DateTime" -> "datetime-local"
                        | _          -> "text"
                else
                    "text"                

            let attrs_b =
                (
                    if not type_attribute_provided then
                        [ _type type_value ]
                    else
                        []                        
                )
                @
                [ attr "data-val" "true"]
                @
                (
                    match property_info.PropertyType.Name with
                    | "Int32"
                    | "DateTime" -> [ attr "data-val-required" (sprintf "The %s field is required." display_name) ]
                    | "Decimal"  ->
                        [
                            attr "data-val-number" (sprintf "The field %s must be a number." display_name)
                            attr "data-val-required" (sprintf "The %s field is required." display_name)
                        ]
                    | _ -> []
                )
                @
                (
                    let cattr = System.Attribute.GetCustomAttribute(property_info, typedefof<StringLengthAttribute>) :?> StringLengthAttribute

                    if (not (isNull cattr)) then
                        if (cattr.MinimumLength > 0) then
                            [
                                attr "data-val-length" (sprintf "The field %s must be a string with a minimum length of %i and a maximum length of %i." property_info.Name cattr.MinimumLength cattr.MaximumLength)
                                attr "data-val-length-max" (string cattr.MaximumLength)
                                attr "data-val-length-min" (string cattr.MinimumLength)
                                attr "maxlength" (string cattr.MaximumLength)
                            ]
                        else
                            [
                                attr "data-val-length" (sprintf "The field %s must be a string with a maximum length of %i." property_info.Name cattr.MaximumLength)
                                attr "data-val-length-max" (string cattr.MaximumLength)
                                attr "maxlength" (string cattr.MaximumLength)
                            ]
                    else []
                )
                @
                (
                    let cattr = System.Attribute.GetCustomAttribute(property_info, typedefof<RegularExpressionAttribute>) :?> RegularExpressionAttribute

                    if (not (isNull cattr)) then
                        [
                            attr "data-val-regex" (sprintf "The field %s must match the regular expression %s." property_info.Name cattr.Pattern)
                            attr "data-val-regex-pattern" cattr.Pattern
                        ]
                    else []
                )
                @
                (
                    let cattr = System.Attribute.GetCustomAttribute(property_info, typedefof<RangeAttribute>) :?> RangeAttribute

                    if (not (isNull cattr)) then
                        [
                            attr "data-val-range" (sprintf "The field %s must be between %s and %s." property_info.Name (string cattr.Minimum) (string cattr.Maximum))
                            attr "data-val-range-max" (string cattr.Maximum)
                            attr "data-val-range-min" (string cattr.Minimum)
                        ]
                    else []
                )
                @
                (
                    let cattr = System.Attribute.GetCustomAttribute(property_info, typedefof<RequiredAttribute>) :?> RequiredAttribute

                    if (not (isNull cattr)) then
                        [ attr "data-val-required" (sprintf "The %s field is required." property_info.Name) ]
                    else []                    
                )
                @
                [
                    _id property_info.Name
                    _name property_info.Name
                    _value
                        (
                            if type_value = "date" then
                                if (isNull (get_current_value())) then
                                    ""
                                else
                                    (get_current_value() :?> System.DateTime).ToString "yyyy-MM-dd"
                            else
                                (string (get_current_value()))
                        )

                ]                    
            
            input (attrs_a @ attrs_b)
            
        | _ -> failwith "tag helper issue"

// ----------------------------------------------------------------------

[<RequireQualifiedAccess>]
type SpanValidation =
    static member Of([<ReflectedDefinition>] expr: Expr<'a>, attrs_a: XmlAttribute list) =
        match expr with
        | PropInfo(property_info, get_current_value) ->

            let attrs_d =

                let has_class =
                    attrs_a.Any(fun xml_attr ->
                        match xml_attr with
                        | KeyValue (attr_key, attr_val) ->
                            if attr_key = "class" then
                                true
                            else
                                false
                        | Boolean str -> false
                    )

                if has_class then
                    attrs_a
                else
                    attrs_a @ [ _class "" ]


            let attrs_b = 
                attrs_d.Select(fun xml_attr -> 
                    match xml_attr with
                    | KeyValue (attr_key, attr_val) ->
                        if attr_key = "class" then
                            KeyValue (attr_key, attr_val + " field-validation-valid")
                        else
                            xml_attr
                    | Boolean str -> xml_attr
                )
            
            let attrs_c =
                [
                    attr "data-valmsg-for" property_info.Name
                    attr "data-valmsg-replace" "true"
                ]

            span ((List.ofSeq attrs_b) @ attrs_c) []
            
        | _ -> failwith "tag helper issue"

// ----------------------------------------------------------------------

[<RequireQualifiedAccess>]
type Label =
    static member Of([<ReflectedDefinition>] expr: Expr<'a>, attrs_a: XmlAttribute list) =
        match expr with
        | PropInfo(property_info, _) ->

            let display_name =

                let cattr = System.Attribute.GetCustomAttribute(property_info, typedefof<DisplayAttribute>) :?> DisplayAttribute

                if (not (isNull cattr)) then
                    cattr.Name
                else
                    property_info.Name        
            
            // System.Console.WriteLine(property_name + " : " + property_info.PropertyType.Name)

            label (attrs_a @ [ _for property_info.Name ]) [ encodedText display_name ]
        
        | _ -> failwith "tag helper issue"

// ----------------------------------------------------------------------

[<RequireQualifiedAccess>]
type Display =
    static member NameFor([<ReflectedDefinition>] expr: Expr<'a>) =
        match expr with
        | PropInfo(property_info, _) ->

            let display_name =

                let cattr = System.Attribute.GetCustomAttribute(property_info, typedefof<DisplayAttribute>) :?> DisplayAttribute

                if (not (isNull cattr)) then
                    cattr.Name
                else
                    property_info.Name        

            display_name
                    
        | _ -> failwith "tag helper issue"    

    static member For([<ReflectedDefinition>] expr: Expr<'a>) =
        match expr with
        | PropInfo(property_info, get_current_value) ->
                    
            let data_type_attr = System.Attribute.GetCustomAttribute(property_info, typedefof<DataTypeAttribute>) :?> DataTypeAttribute

            if (isNull data_type_attr) then
                get_current_value() |> string
            elif data_type_attr.DataType = DataType.Date then
                (get_current_value() :?> System.DateTime).ToString "d"
            elif data_type_attr.DataType = DataType.Currency then
                (get_current_value() :?> System.Decimal).ToString "C"
            else
                get_current_value() |> string
         
        | _ -> failwith "tag helper issue"    
