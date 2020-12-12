module MvcMovieGiraffe.TagHelpers

open System.Linq

open System.ComponentModel.DataAnnotations

open Giraffe.ViewEngine

// let input (type_obj : System.Type) (property_name : string) (value_str : string) (name_str : string) (attrs_a : XmlAttribute list) =

let input (type_obj : System.Type) (property_name : string) (value_str : string) (attrs_a : XmlAttribute list) =

    let mutable type_value = ""

    let mutable ls : XmlAttribute list = []

    let properties = type_obj.GetProperties()

    let property_info = properties.First(fun info -> info.Name = property_name)

    let mutable display_name = property_name

    let _ =

        let cattr = System.Attribute.GetCustomAttribute(property_info, typedefof<DisplayAttribute>) :?> DisplayAttribute

        if (not (isNull cattr)) then
            display_name <- cattr.Name

    // System.Console.WriteLine(property_name + " : " + property_info.PropertyType.Name)

    if (property_info.PropertyType.Name = "Int64") then
        type_value <- "number"
    elif (property_info.PropertyType.Name = "DateTime") then
        type_value <- "datetime-local"
        ls <- ls @ [ attr "data-val-required" (sprintf "The %s field is required." display_name) ]
    elif (property_info.PropertyType.Name = "String") then        
        type_value <- "text"
    elif (property_info.PropertyType.Name = "Decimal") then
        type_value <- "text"
        ls <- ls @ [ attr "data-val-number" (sprintf "The field %s must be a number." display_name) ]
        ls <- ls @ [ attr "data-val-required" (sprintf "The %s field is required." display_name) ]
    else
        type_value <- "text"

    let _ =

        let cattr = System.Attribute.GetCustomAttribute(property_info, typedefof<DataTypeAttribute>) :?> DataTypeAttribute

        if (not (isNull cattr)) then
            if (cattr.DataType = DataType.Date) then
                type_value <- "date"
  
    let _ =

        let cattr = System.Attribute.GetCustomAttribute(property_info, typedefof<StringLengthAttribute>) :?> StringLengthAttribute

        if (not (isNull cattr)) then
            ls <- ls @ [ attr "data-val-length" (sprintf "The field %s must be a string with a minimum length of %i and a maximum length of %i." property_name cattr.MinimumLength cattr.MaximumLength) ]
            ls <- ls @ [ attr "data-val-length-max" (string cattr.MaximumLength) ]
            ls <- ls @ [ attr "data-val-length-min" (string cattr.MinimumLength) ]
            ls <- ls @ [ attr "maxlength" (string cattr.MaximumLength) ]

    let _ =

        let cattr = System.Attribute.GetCustomAttribute(property_info, typedefof<RegularExpressionAttribute>) :?> RegularExpressionAttribute

        if (not (isNull cattr)) then

            ls <- ls @ [ attr "data-val-regex" (sprintf "The field %s must match the regular expression %s." property_name cattr.Pattern) ]
            ls <- ls @ [ attr "data-val-regex-pattern" cattr.Pattern ]

    let _ =

        let cattr = System.Attribute.GetCustomAttribute(property_info, typedefof<RangeAttribute>) :?> RangeAttribute

        if (not (isNull cattr)) then

            ls <- ls @ [ attr "data-val-range" (sprintf "The field %s must be between %s and %s." property_name (string cattr.Minimum) (string cattr.Maximum)) ]
            ls <- ls @ [ attr "data-val-range-max" (string cattr.Maximum) ]
            ls <- ls @ [ attr "data-val-range-min" (string cattr.Minimum) ]        
            

    let _ =

        let cattr = System.Attribute.GetCustomAttribute(property_info, typedefof<RequiredAttribute>) :?> RequiredAttribute

        if (not (isNull cattr)) then
            ls <- ls @ [ attr "data-val-required" (sprintf "The %s field is required." property_name) ]

    let attrs_b = 
        [
            _type type_value
            attr "data-val" "true"
        ] 

    let attrs_d = 
        [
            _id property_name
            _name property_name
            _value value_str                                        
        ]                                 
    
    input (attrs_a @ attrs_b @ ls @ attrs_d)
