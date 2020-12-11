module MvcMovieGiraffe.App

open System
open System.IO
open System.Linq

open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting

open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Mvc.Rendering

open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open System.ComponentModel.DataAnnotations
open System.ComponentModel.DataAnnotations.Schema

open Microsoft.EntityFrameworkCore

open Giraffe

// ---------------------------------
// Models
// ---------------------------------

[<CLIMutable>]
type Movie =
    {
        Id : int

        [<StringLength(60, MinimumLength = 3)>]
        [<Required>]
        Title : string

        [<Display(Name = "Release Date")>]
        [<DataType(DataType.Date)>]
        ReleaseDate : DateTime

        [<RegularExpression(@"^[A-Z]+[a-zA-Z]*$")>]
        [<Required>]
        Genre : string

        [<Range(1, 100)>]
        // [<DataType(DataType.Currency)>]
        [<Column(TypeName = "decimal(18, 2)")>]
        Price : decimal

        [<RegularExpression(@"^[A-Z]+[a-zA-Z0-9-]*$")>]
        [<StringLength(5)>]
        [<Required>]
        Rating : string
    }

[<CLIMutable>]
type MovieGenreViewModel = {
    Movies : System.Collections.Generic.List<Movie>
    Genres : SelectList
    MovieGenre : string
    SearchString : string
}

// --------------------------------------------------------------------------------

type MvcMovieContext(options : DbContextOptions<MvcMovieContext>) =
    inherit DbContext(options)

    [<DefaultValue>]
    val mutable Movie : DbSet<Movie>

    member public this._Movie
        with get ()      = this.Movie
        and  set (value) = this.Movie <- value

module DataContextInitialize =

    let Initialize (context : MvcMovieContext) =
        context.Database.EnsureCreated() |> ignore

        context.Movie.AddRange(
            {
                Id = 0 
                Title = "Enter the Dragon"
                ReleaseDate = DateTime.Parse("1973-08-19")
                Genre = "Martial Arts"
                Price = 7.99M
                Rating = "R"
            },
            
            {
                Id = 0
                Title = "The Twilight Samurai"
                ReleaseDate = DateTime.Parse("2002-11-02")
                Genre = "Samurai"
                Price = 8.99M
                Rating = "R"
            },

            
            {
                Id = 0
                Title = "Ford v Ferrari"
                ReleaseDate = DateTime.Parse("2019-11-15")
                Genre = "Racing"
                Price = 9.99M
                Rating = "PG-13"
            },

            {
                Id = 0
                Title = "War Games"
                ReleaseDate = DateTime.Parse("1983-06-03")
                Genre = "Computer Hacker"
                Price = 3.99M
                Rating = "PG"
            },
            
            {
                Id = 0
                Title = "Rio Bravo"
                ReleaseDate = DateTime.Parse("1959-4-15")
                Genre = "Western"
                Price = 3.99M
                Rating = "G"
            }            
        ) |> ignore

        context.SaveChanges() |> ignore

// ---------------------------------
// Views
// ---------------------------------

module Views =
    open Giraffe.ViewEngine

    let layout (title_data: string) (content: XmlNode list) =
        html [] [
            head [] [
                title []  [ 
                    encodedText title_data 
                    encodedText " - MvcMovieGiraffe"
                ]
                link [ _rel "stylesheet"; _type "text/css"; _href "/lib/bootstrap/css/bootstrap.min.css" ]
                link [ _rel "stylesheet"; _type "text/css"; _href "/main.css" ]
            ]
            body [] [ 
                header [] [
                    nav [ _class "navbar navbar-expand-sm navbar-toggleable-sm navbar-light bg-white border-bottom box-shadow mb-3" ] [
                        div [ _class "container" ] [
                            a [ _class "navbar-brand"; _href "/" ] [ encodedText "MvcMovieGiraffe" ]
                            button [ 
                                _class "navbar-toggler"
                                _type "button"
                                attr "data-toggle" "collapse"
                                attr "data-target" ".navbar-collapse"
                                attr "aria-controls" "navbarSupportedContent"
                                attr "aria-expanded" "false"
                                attr "aria-label" "Toggle navigation"
                            ] [
                                span [ _class "navbar-toggler-icon" ] []
                            ]
                            div [ _class "navbar-collapse collapse d-sm-inline-flex flex-sm-row-reverse" ] [
                                ul [ _class "navbar-nav flex-grow-1" ] [
                                    li [ _class "nav-link dark-text" ] [ a [ _href "/"             ] [ encodedText "Home"   ] ]
                                    li [ _class "nav-link dark-text" ] [ a [ _href "/Home/Privacy" ] [ encodedText "Privacy"] ]
                                ]
                            ]
                        ]
                    ]
                ]
                
                div [ _class "container" ] [ main [ attr "role" "main"; _class "pb-3" ] content ]

                footer [ _class "border-top footer text-muted" ] [
                    div [ _class "container" ] [
                        rawText "&copy; "
                        encodedText "2020 - WebApplicationCs - "
                        a [ _href "/Home/Privacy" ] [ encodedText "Privacy" ]
                    ]
                ]

                script [ _src "/lib/jquery/jquery.min.js" ] []
                script [ _src "/lib/bootstrap/js/bootstrap.bundle.min.js" ] [] 
            ]
        ]

    let partial () =
        h1 [] [ encodedText "MvcMovieGiraffe" ]

    let movies (model : MovieGenreViewModel) =
        [
            h1 [] [ encodedText "Index" ]

            p [] [ a [ _href "/Movies/Create" ] [ encodedText "Create New" ] ]

            form [ _method "get"; _action "/Movies" ] [
                p [] [

                    select [ _id "MovieGenre"; _name "MovieGenre" ] (
                        [ option [ _value "" ] [ encodedText "All" ] ]
                        @
                        (Seq.toList (model.Genres.ToList().Select(fun elt -> 
                            option [] [ encodedText elt.Text ]).ToList()))
                    )

                    encodedText "Title:"

                    input [ _type "text"; _name "SearchString" ]

                    input [ _type "submit"; _value "Filter" ]
                ]
            ]

            table [ _class "table" ] [
                thead [] [
                    tr [] [
                        th [] [ encodedText "Title" ]
                        th [] [ encodedText "ReleaseDate" ]
                        th [] [ encodedText "Genre" ]
                        th [] [ encodedText "Price" ]
                        th [] [ encodedText "Rating" ]
                        th [] []
                    ]
                ]

                tbody [] (Seq.toList (model.Movies.ToList().Select(fun elt -> 
                    tr [] [
                        td [] [ encodedText elt.Title ]
                        td [] [ encodedText (string elt.ReleaseDate) ]
                        td [] [ encodedText elt.Genre ]
                        td [] [ encodedText (string elt.Price) ]
                        td [] [ encodedText elt.Rating ]

                        td [] [
                            a [ _href ("/Movies/Edit/"    + (string elt.Id)) ] [ encodedText "Edit" ]
                            encodedText " | "
                            a [ _href ("/Movies/Details/" + (string elt.Id)) ] [ encodedText "Details" ]
                            encodedText " | "
                            a [ _href ("/Movies/Delete/"  + (string elt.Id)) ] [ encodedText "Delete" ]
                        ]
                    ])))
            ]
        ] |> layout "Index"
    
    let details (model : Movie) = 
        [

            h1 [] [ encodedText "Details" ]

            div [] [

                h4 [] [ encodedText "Movie" ]

                hr []

                dl [ _class "row" ] [
                    dt [ _class "col-sm-2" ] [ encodedText "Title" ]
                    dd [ _class "col-sm-10"] [ encodedText model.Title ]

                    dt [ _class "col-sm-2" ] [ encodedText "ReleaseDate" ]
                    dd [ _class "col-sm-10"] [ encodedText (string model.ReleaseDate) ]

                    dt [ _class "col-sm-2" ] [ encodedText "Genre" ]
                    dd [ _class "col-sm-10"] [ encodedText model.Genre ]

                    dt [ _class "col-sm-2" ] [ encodedText "Price" ]
                    dd [ _class "col-sm-10"] [ encodedText (string model.Price) ]

                    dt [ _class "col-sm-2" ] [ encodedText "Rating" ]
                    dd [ _class "col-sm-10"] [ encodedText model.Rating ]
                ]
            ]

            div [] [
                a [ _href ("/Movies/Edit/" + (string model.Id)) ] [ encodedText "Edit" ]

                encodedText "|"

                a [ _href "/Movies" ] [ encodedText "Back to list" ]
            ]
        ] |> layout "Details"

    let create =
        [
            h1 [] [ encodedText "Create" ]

            h4 [] [ encodedText "Movie" ]

            hr []

            div [ _class "row" ] [
                div [ _class "col-md-4" ] [
                    form [ _action "/Movies/Create"; _method "post" ] [

                        div [ _class "form-group" ] [
                            label [ _class "control-label"; _for "Title" ] [ encodedText "Title" ]

                            input [ 
                                _class "form-control"
                                _type "text"
                                attr "data-val" "true" 
                                attr "data-val-length" "The field Title must be a string with a minimum length of 3 and a maximum length of 60."
                                attr "data-val-length-max" "60" 
                                attr "data-val-length-min" "3" 
                                attr "data-val-required" "The Title field is required."
                                _id "Title"
                                _maxlength "60"
                                _name "Title"
                                _value ""
                            ]

                            span [ 
                                _class "text-danger field-validation-valid" 
                                attr "data-valmsg-for" "Title"
                                attr "data-valmsg-replace" "true"
                            ] []
                        ]

                        div [ _class "form-group" ] [
                            label [ _class "control-label"; _for "ReleaseDate" ] [ encodedText "ReleaseDate" ]

                            input [ 
                                _class "form-control"
                                _type "date"
                                attr "data-val" "true" 
                                attr "data-val-required" "The Release Date field is required."
                                _id "ReleaseDate"
                                _name "ReleaseDate"
                                _value ""
                            ]

                            span [ 
                                _class "text-danger field-validation-valid" 
                                attr "data-valmsg-for" "ReleaseDate"
                                attr "data-valmsg-replace" "true"
                            ] []
                        ]

                        div [ _class "form-group" ] [
                            label [ _class "control-label"; _for "Genre" ] [ encodedText "Genre" ]

                            input [ 
                                _class "form-control"
                                _type "text"
                                attr "data-val" "true" 
                                attr "data-val-regex" "The field Genre must match the regular expression ^[A-Z]+[a-zA-Z]*$"
                                attr "data-val-required" "The Genre field is required."
                                _id "Genre"
                                _name "Genre"
                                _value ""
                            ]

                            span [ 
                                _class "text-danger field-validation-valid" 
                                attr "data-valmsg-for" "Genre"
                                attr "data-valmsg-replace" "true"
                            ] []
                        ]

                        div [ _class "form-group" ] [
                            label [ _class "control-label"; _for "Price" ] [ encodedText "Price" ]

                            input [ 
                                _class "form-control"
                                _type "text"
                                attr "data-val" "true" 
                                attr "data-val-number" "The field Price must be a number."
                                attr "data-val-range" "The field Price must be between 1 and 100."
                                attr "data-val-range-max" "100"
                                attr "data-val-range-min" "1"

                                attr "data-val-required" "The Price field is required."

                                _id "Price"
                                _name "Price"
                                _value ""
                            ]

                            span [ 
                                _class "text-danger field-validation-valid" 
                                attr "data-valmsg-for" "Price"
                                attr "data-valmsg-replace" "true"
                            ] []
                        ]

                        div [ _class "form-group" ] [
                            label [ _class "control-label"; _for "Rating" ] [ encodedText "Rating" ]

                            input [ 
                                _class "form-control"
                                _type "text"
                                attr "data-val" "true" 

                                attr "data-val-length" "The field Rating must be a string with a maximum length of 5." 
                                attr "data-val-length-max" "5" 
                                attr "data-val-regex" "The field Rating must match the regular expression ^[A-Z]+[a-zA-Z0-9-]*$." 
                                attr "data-val-regex-pattern" "^[A-Z]&#x2B;[a-zA-Z0-9-]*$"
                                
                                attr "data-val-required" "The Rating field is required."

                                _id "Rating"
                                _maxlength "5"
                                _name "Rating"
                                _value ""
                            ]

                            span [ 
                                _class "text-danger field-validation-valid" 
                                attr "data-valmsg-for" "Rating"
                                attr "data-valmsg-replace" "true"
                            ] []
                        ]

                        div [ _class "form-group" ] [
                            input [ _type "submit"; _value "Create"; _class "btn btn-primary" ]
                        ]

                        input [ 
                            _name "__RequestVerificationToken"
                            _type "hidden"
                            _value "..."
                        ]
                    ]
                ]
            ]
        
            div [ _href "/Movies" ] [ encodedText "Back to List" ]

        ] |> layout "Create"

    let edit (model : Movie) =
        [
            h1 [] [ encodedText "Edit" ]

            h4 [] [ encodedText "Movie" ]

            hr []

            div [ _class "row" ] [
                div [ _class "col-md-4" ] [
                    form [ _action ("/Movies/Edit/" + (string model.Id)); _method "post" ] [

                        input [ 
                            _type "hidden"
                            attr "data-val" "true"
                            attr "data-val-required" "The Id field is required."
                            _id "Id"
                            _name "Id"
                            _value (string model.Id)
                        ]

                        // div [ _class "form-group" ] [
                        //     label [ _class "control-label"; _for "Title" ] [ encodedText "Title"]
                        //     input [
                        //         _class "form-control"
                        //         _type "text"
                        //         attr "data-val" "true"
                        //         attr "data-val-length" "The field Title must be a string with a minimum length of 3 and a maximum length of 60."
                        //         attr "data-val-length-max" "60" 
                        //         attr "data-val-length-min" "3" 
                        //         attr "data-val-required" "The Title field is required." 
                        //         _id "Title"
                        //         _maxlength "60" 
                        //         _name "Title"
                        //         _value model.Title
                        //     ]
                        //     span [
                        //         _class "text-danger field-validation-valid"
                        //         attr "data-valmsg-for" "Title"
                        //         attr "data-valmsg-replace" "true"
                        //     ] []
                        // ]                        

                        let form_group (name : string) (type_name : string) (value_str : string) (input_attrs : XmlAttribute list) =
                            div [ _class "form-group" ] [

                                label [ _class "control-label"; _for name ] [ encodedText name ]

                                let input_attrs_1 = [
                                    _class "form-control"; 
                                    _type type_name
                                    attr "data-val" "true"
                                ]

                                let input_attrs_2 = [
                                    attr "data-val-required" (sprintf "The %s field is required." name)
                                    _id name
                                    _name name
                                    _value value_str
                                ]

                                input (input_attrs_1 @ input_attrs @ input_attrs_2)
                                
                                span [ 
                                    _class "text-danger field-validation-valid"
                                    attr "data-valmsg-for" name
                                    attr "data-valmsg-replace" "true"                                    
                                ] []
                            ]

                        form_group "Title" "text" model.Title [
                            attr "data-val-length" "The field Title must be a string with a minimum length of 3 and a maximum length of 60."
                            attr "data-val-length-max" "60" 
                            attr "data-val-length-min" "3"
                            _maxlength "60"
                        ]

                        // form_group "Release Date" "date" (string model.ReleaseDate) []

                        form_group "ReleaseDate" "date" (model.ReleaseDate.ToString "yyyy-MM-dd") []

                        form_group "Genre" "text" model.Genre [
                            attr "data-val-regex" "The field Genre must match the regular expression ^[A-Z]+[a-zA-Z]*$."
                            attr "data-val-regex-pattern" "^[A-Z]+[a-zA-Z]*$"
                        ]

                        form_group "Price" "text" (string model.Price) [ ]

                        form_group "Rating" "text" model.Rating [ ]

                        div [ _class "form-group" ] [
                            input [ _type "submit"; _value "Save"; _class "btn btn-primary" ]
                        ]

                        input [ _name "__RequestVerificationToken"; _type "hidden"; _value "..." ]
                    ]
                ]
            ]

            div [] [
                a [ _href "/Movies" ] [ encodedText "Back to List" ]
            ]
        ] |> layout "Edit"

    let delete (model : Movie) = 
        [
            h1 [] [ encodedText "Delete" ]

            h3 [] [ encodedText "Are you sure you want to delete this?" ]

            div [] [
                h4 [] [ encodedText "Movie" ]
                hr []

                // dl [ _class "row" ] [
                //     // dt [ _class "col-sm-2" ] [ encodedText "Title" ]
                //     // dd [ _class "col-sm-10" ] [ encodedText model.Title ]                    
                // ]

                let entry (label_str : string) (value_str : string) =
                    [
                        dt [ _class "col-sm-2" ] [ encodedText label_str ]
                        dd [ _class "col-sm-10" ] [ encodedText value_str ]
                    ]                                

                dl [ _class "row" ] (
                    (entry "Title" model.Title) @ 
                    (entry "Release Date" (string model.ReleaseDate)) @
                    (entry "Genre" model.Genre) @
                    (entry "Price" (string model.Price)) @
                    (entry "Rating" model.Rating)
                )

                form [ _action ("/Movies/Delete/" + (string model.Id)); _method "post" ] [
                    input [ 
                        _type "hidden"
                        attr "data-val" "true" 
                        attr "data-val-required" "The Id field is required." 
                        _id "Id"
                        _name "Id"
                        _value (string model.Id)
                    ]

                    input [ _type "submit"; _value "Delete"; _class "btn btn-danger" ]

                    a [ _href "/Movies" ] [ encodedText "Back to List" ]

                    input [ _name "__RequestVerificationToken"; _type "hidden"; _value "..." ]
                ]
            ]
        ] |> layout "Delete"

// ---------------------------------
// Web app
// ---------------------------------

let movies_handler : HttpHandler = 
    fun  (next : HttpFunc) (ctx : HttpContext) ->

        let context = ctx.RequestServices.GetService(typeof<MvcMovieContext>) :?> MvcMovieContext

        let movieGenre =
            match ctx.TryGetQueryStringValue "movieGenre" with
            | None -> ""
            | Some str -> str

        let searchString =
            match ctx.TryGetQueryStringValue "searchString" with
            | None -> ""
            | Some str -> str

        let mutable movies = query {
            for elt in context.Movie do
                select elt
        }

        if not (String.IsNullOrEmpty(searchString)) then
            movies <- query {
                for elt in movies do
                    where (elt.Title.Contains(searchString))
                    select elt
            }
        
        if not (String.IsNullOrEmpty(movieGenre)) then
            movies <- query {
                for elt in movies do
                    where (elt.Genre = movieGenre)
                    select elt
            }        
                
        let view = Views.movies {
            Genres = new SelectList(context.Movie
                .OrderBy(fun elt -> elt.Genre)
                .Select(fun elt -> elt.Genre)
                .Distinct()
                .ToList())
                
            Movies = movies.ToList()
            MovieGenre = null
            SearchString = null            
        }

        htmlView view next ctx

let details_handler (id : int) : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        
        let context = ctx.RequestServices.GetService(typeof<MvcMovieContext>) :?> MvcMovieContext

        let movie = context.Movie.FirstOrDefault(fun elt -> elt.Id = id)

        if (isNull (box movie)) then
            RequestErrors.notFound (text "Not Found") next ctx
        else
            let view = Views.details movie

            htmlView view next ctx

let create_handler : HttpHandler =
    fun  (next : HttpFunc) (ctx : HttpContext) ->

        let view = Views.create

        htmlView view next ctx

let post_create_handler : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        
        let movie = {
            Id = 0
            Title = ctx.Request.Form.Item("Title").ToString()
            ReleaseDate = DateTime.Parse(ctx.Request.Form.Item("ReleaseDate").ToString())
            Genre = ctx.Request.Form.Item("Genre").ToString()
            Price = (decimal (ctx.Request.Form.Item("Price").ToString()))
            Rating = ctx.Request.Form.Item("Rating").ToString()
        }

        let context = ctx.RequestServices.GetService(typeof<MvcMovieContext>) :?> MvcMovieContext
        
        context.Add(movie) |> ignore
        context.SaveChanges() |> ignore

        htmlView (Giraffe.ViewEngine.HtmlElements.encodedText "post - ok")  next ctx

let edit_handler (id : int) : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->

        let context = ctx.RequestServices.GetService(typeof<MvcMovieContext>) :?> MvcMovieContext

        let movie = context.Movie.Find(id)

        if (isNull (box movie)) then
            RequestErrors.notFound (text "Not Found") next ctx
        else
            let view = Views.edit movie
            htmlView view next ctx

let post_edit_handler (id : int) : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
                
        let movie = {
            Id = (int (ctx.Request.Form.Item("Id").ToString()))
            Title = ctx.Request.Form.Item("Title").ToString()
            ReleaseDate = DateTime.Parse(ctx.Request.Form.Item("ReleaseDate").ToString())
            Genre = ctx.Request.Form.Item("Genre").ToString()
            Price = (decimal (ctx.Request.Form.Item("Price").ToString()))
            Rating = ctx.Request.Form.Item("Rating").ToString()
        }

        if id <> movie.Id then
            htmlView (Giraffe.ViewEngine.HtmlElements.encodedText "edit - not found")  next ctx
        else
            let context = ctx.RequestServices.GetService(typeof<MvcMovieContext>) :?> MvcMovieContext

            try
                context.Update(movie) |> ignore
                context.SaveChanges() |> ignore
                htmlView (Giraffe.ViewEngine.HtmlElements.encodedText "edit - ok")  next ctx
            with
                | :? DbUpdateConcurrencyException as ex ->
                failwith "edit - issue"

let delete_handler (id : int) : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        
        let context = ctx.RequestServices.GetService(typeof<MvcMovieContext>) :?> MvcMovieContext

        let movie = context.Movie.FirstOrDefault(fun elt -> elt.Id = id)

        if (isNull (box movie)) then
            RequestErrors.notFound (text "delete - Not Found") next ctx
        else
            let view = Views.delete movie

            htmlView view next ctx           

let post_delete_handler (id : int) : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->

        let context = ctx.RequestServices.GetService(typeof<MvcMovieContext>) :?> MvcMovieContext

        let movie = context.Movie.Find(id)

        context.Movie.Remove(movie) |> ignore
        context.SaveChanges()       |> ignore

        // redirect "/Movies"

        htmlView (Giraffe.ViewEngine.HtmlElements.encodedText "delete - ok")  next ctx
       
let webApp =
    choose [
        GET >=>
            choose [
                route "/Movies" >=> movies_handler

                routef "/Movies/Details/%i" details_handler

                route "/Movies/Create" >=> create_handler

                routef "/Movies/Edit/%i" edit_handler

                routef "/Movies/Delete/%i" delete_handler
            ]
                
        POST >=> choose [ 
            route  "/Movies/Create"  >=> post_create_handler
            routef "/Movies/Edit/%i"     post_edit_handler
            routef "/Movies/Delete/%i"   post_delete_handler
        ]

        setStatusCode 404 >=> text "Not Found" ]

// ---------------------------------
// Error handler
// ---------------------------------

let errorHandler (ex : Exception) (logger : ILogger) =
    logger.LogError(ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> text ex.Message

// ---------------------------------
// Config and Main
// ---------------------------------

let configureCors (builder : CorsPolicyBuilder) =
    builder.WithOrigins("http://localhost:8080")
           .AllowAnyMethod()
           .AllowAnyHeader()
           |> ignore

let configureApp (app : IApplicationBuilder) =
    let env = app.ApplicationServices.GetService<IWebHostEnvironment>()
    (match env.EnvironmentName with
    | "Development" -> app.UseDeveloperExceptionPage()
    | _ -> app.UseGiraffeErrorHandler(errorHandler))
        // .UseHttpsRedirection()
        .UseCors(configureCors)
        .UseStaticFiles()
        .UseGiraffe(webApp)

let configureServices (services : IServiceCollection) =
    services.AddCors()    |> ignore
    services.AddGiraffe() |> ignore
    services.AddDbContext<MvcMovieContext>(fun options ->
            // options.UseSqlite(Configuration.GetConnectionString("MvcMovieContext"))
            // options.UseSqlite("Data Source=MvcMovie.db") |> ignore
            options.UseInMemoryDatabase("DB_ToDo") |> ignore
        ) |> ignore

let configureLogging (builder : ILoggingBuilder) =
    builder.AddFilter(fun l -> l.Equals LogLevel.Error)
           .AddConsole()
           .AddDebug() |> ignore

[<EntryPoint>]
let main args =
    let contentRoot = Directory.GetCurrentDirectory()
    let webRoot     = Path.Combine(contentRoot, "WebRoot")

    let host = Host.CreateDefaultBuilder(args).ConfigureWebHostDefaults(fun webHostBuilder ->
        webHostBuilder
            .UseContentRoot(contentRoot)
            .UseWebRoot(webRoot)
            .Configure(Action<IApplicationBuilder> configureApp)
            .ConfigureServices(configureServices)
            .ConfigureLogging(configureLogging)
            |> ignore).Build()

    use scope = host.Services.CreateScope()
    let services = scope.ServiceProvider
    let context = services.GetRequiredService<MvcMovieContext>()

    DataContextInitialize.Initialize(context) |> ignore
        
    host.Run()

    0