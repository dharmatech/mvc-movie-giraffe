module MvcMovieGiraffe.TagHelpers

open System.Linq

open System.ComponentModel.DataAnnotations

open Giraffe.ViewEngine

let input (type_obj : System.Type) (property_name : string) (value_str : string) (name_str : string) (attrs_a : XmlAttribute list)=

    let mutable type_value = ""

    let mutable ls : XmlAttribute list = []

    let properties = type_obj.GetProperties()

    let property_info = properties.First(fun info -> info.Name = property_name)

    if (property_info.PropertyType.Name = "Int64") then
        type_value <- "number"
    else
        type_value <- "text"

    let _ =

        let cattr = System.Attribute.GetCustomAttribute(property_info, typedefof<StringLengthAttribute>) :?> StringLengthAttribute

        if (not (isNull cattr)) then
            ls <- ls @ [ attr "data-val-length" (sprintf "The field %s must be a string with a minimum length of %i and a maximum length of %i." property_name cattr.MinimumLength cattr.MaximumLength) ]
            ls <- ls @ [ attr "data-val-length-max" (string cattr.MaximumLength) ]
            ls <- ls @ [ attr "data-val-length-min" (string cattr.MinimumLength) ]
            ls <- ls @ [ attr "maxlength" (string cattr.MaximumLength) ]

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
            _id name_str
            _name name_str
            _value value_str                                        
        ]                                 
    
    input (attrs_a @ attrs_b @ ls @ attrs_d)
