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
            
            let mutable ls : XmlAttribute list = []
                                    
            let display_name =

                let cattr = System.Attribute.GetCustomAttribute(property_info, typedefof<DisplayAttribute>) :?> DisplayAttribute

                if (not (isNull cattr)) then
                    cattr.Name
                else
                    property_info.Name

            // System.Console.WriteLine(property_name + " : " + property_info.PropertyType.Name)

            let mutable type_value = ""

            let type_attribute_provided = attrs_a.Any(fun xml_attr ->
                match xml_attr with
                | KeyValue (attr_key, attr_val) -> attr_key = "type"
                | Boolean str -> false)

            if not type_attribute_provided then
                if (property_info.PropertyType.Name = "Int64") then
                    type_value <- "number"
                elif (property_info.PropertyType.Name = "DateTime") then
                    type_value <- "datetime-local"
                elif (property_info.PropertyType.Name = "String") then        
                    type_value <- "text"
                elif (property_info.PropertyType.Name = "Decimal") then
                    type_value <- "text"
                else
                    type_value <- "text"        

            if (property_info.PropertyType.Name = "Int32") then
                ls <- ls @ [ attr "data-val-required" (sprintf "The %s field is required." display_name) ]
            if (property_info.PropertyType.Name = "DateTime") then
                ls <- ls @ [ attr "data-val-required" (sprintf "The %s field is required." display_name) ]
            elif (property_info.PropertyType.Name = "Decimal") then
                ls <- ls @ [ attr "data-val-number" (sprintf "The field %s must be a number." display_name) ]
                ls <- ls @ [ attr "data-val-required" (sprintf "The %s field is required." display_name) ]
            
            let _ =

                let cattr = System.Attribute.GetCustomAttribute(property_info, typedefof<DataTypeAttribute>) :?> DataTypeAttribute

                if (not (isNull cattr)) then
                    if (cattr.DataType = DataType.Date) then
                        if not type_attribute_provided then
                            type_value <- "date"
          
            let _ =

                let cattr = System.Attribute.GetCustomAttribute(property_info, typedefof<StringLengthAttribute>) :?> StringLengthAttribute

                if (not (isNull cattr)) then
                    if (cattr.MinimumLength > 0) then
                        ls <- ls @ [ attr "data-val-length" (sprintf "The field %s must be a string with a minimum length of %i and a maximum length of %i." property_info.Name cattr.MinimumLength cattr.MaximumLength) ]
                        ls <- ls @ [ attr "data-val-length-max" (string cattr.MaximumLength) ]
                        ls <- ls @ [ attr "data-val-length-min" (string cattr.MinimumLength) ]
                        ls <- ls @ [ attr "maxlength" (string cattr.MaximumLength) ]                        
                    else 
                        ls <- ls @ [ attr "data-val-length" (sprintf "The field %s must be a string with a maximum length of %i." property_info.Name cattr.MaximumLength) ]
                        ls <- ls @ [ attr "data-val-length-max" (string cattr.MaximumLength) ]
                        ls <- ls @ [ attr "maxlength" (string cattr.MaximumLength) ]        

            let _ =

                let cattr = System.Attribute.GetCustomAttribute(property_info, typedefof<RegularExpressionAttribute>) :?> RegularExpressionAttribute

                if (not (isNull cattr)) then

                    ls <- ls @ [ attr "data-val-regex" (sprintf "The field %s must match the regular expression %s." property_info.Name cattr.Pattern) ]
                    ls <- ls @ [ attr "data-val-regex-pattern" cattr.Pattern ]

            let _ =

                let cattr = System.Attribute.GetCustomAttribute(property_info, typedefof<RangeAttribute>) :?> RangeAttribute

                if (not (isNull cattr)) then

                    ls <- ls @ [ attr "data-val-range" (sprintf "The field %s must be between %s and %s." property_info.Name (string cattr.Minimum) (string cattr.Maximum)) ]
                    ls <- ls @ [ attr "data-val-range-max" (string cattr.Maximum) ]
                    ls <- ls @ [ attr "data-val-range-min" (string cattr.Minimum) ]        
            
            let _ =

                let cattr = System.Attribute.GetCustomAttribute(property_info, typedefof<RequiredAttribute>) :?> RequiredAttribute

                if (not (isNull cattr)) then
                    ls <- ls @ [ attr "data-val-required" (sprintf "The %s field is required." property_info.Name) ]

            let attrs_b = 
                [
                    if not type_attribute_provided then
                        _type type_value
                    attr "data-val" "true"
                ] 
            
            let attrs_d = 
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

            input (attrs_a @ attrs_b @ ls @ attrs_d)
            
        | _ -> encodedText ""

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
            
        | _ -> encodedText ""

// ----------------------------------------------------------------------

[<RequireQualifiedAccess>]
type Label =
    static member Of([<ReflectedDefinition>] expr: Expr<'a>, attrs_a: XmlAttribute list) =
        match expr with
        | PropInfo(property_info, _) ->
                        
            let mutable display_name = property_info.Name

            let _ =

                let cattr = System.Attribute.GetCustomAttribute(property_info, typedefof<DisplayAttribute>) :?> DisplayAttribute

                if (not (isNull cattr)) then
                    display_name <- cattr.Name

            // System.Console.WriteLine(property_name + " : " + property_info.PropertyType.Name)

            label (attrs_a @ [ _for property_info.Name ]) [ encodedText display_name ]
        
        | _ -> failwith "tag helper issue"
