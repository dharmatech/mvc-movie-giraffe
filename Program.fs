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

// open FSharp.Control.Tasks.Builders

open FSharp.Control.Tasks

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
        [<DataType(DataType.Currency)>]
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

        if (context.Movie.Count() = 0) then

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

module Urls =

    let movies         = "/Movies"
    let movies_create  = "/Movies/Create"
    let movies_edit    = "/Movies/Edit/%i"
    let movies_details = "/Movies/Details/%i"
    let movies_delete  = "/Movies/Details/%i"
    
type String with
    member this.route = PrintfFormat<obj, obj, obj, obj, int>(this)
    member this.href = sprintf (Printf.StringFormat<int->string>(this))

module Views =
    open Giraffe.ViewEngine

    let layout (title_data: string) (scripts : XmlNode list) (content: XmlNode list) =
        html [] [
            head [] [
                title []  [ 
                    encodedText title_data 
                    encodedText " - MvcMovieGiraffe"
                ]
                link [ _rel "stylesheet"; _type "text/css"; _href "/lib/bootstrap/css/bootstrap.min.css" ]
                link [ _rel "stylesheet"; _type "text/css"; _href "/main.css" ]
            ]
            body [] ([ 
                header [] [
                    nav [ _class "navbar navbar-expand-sm navbar-toggleable-sm navbar-light bg-white border-bottom box-shadow mb-3" ] [
                        div [ _class "container" ] [
                            a [ _class "navbar-brand"; _href "/Movies" ] [ encodedText "MvcMovieGiraffe" ]
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
                        encodedText "2020 - MvcMovieGiraffe - "
                        a [ _href "/Home/Privacy" ] [ encodedText "Privacy" ]
                    ]
                ]

                script [ _src "/lib/jquery/jquery.min.js" ] []
                script [ _src "/lib/bootstrap/js/bootstrap.bundle.min.js" ] [] 
            ] @ scripts)
        ]
    
    let validation_scripts_partial =
        [
            script [ _src "/lib/jquery-validate/jquery.validate.min.js" ] []
            script [ _src "/lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.min.js" ] []
        ]

    let movies (model : MovieGenreViewModel) =
        [
            h1 [] [ encodedText "Index" ]

            p [] [ a [ _href Urls.movies_create ] [ encodedText "Create New" ] ]

            form [ _method "get"; _action Urls.movies ] [
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
                        th [] [ encodedText (TagHelpers.Display.NameFor(Unchecked.defaultof<Movie>.ReleaseDate)) ]
                        th [] [ encodedText "Genre" ]
                        th [] [ encodedText "Price" ]
                        th [] [ encodedText "Rating" ]
                        th [] []
                    ]
                ]
                
                tbody [] (Seq.toList (model.Movies.ToList().Select(fun elt -> 
                    tr [] [
                        td [] [ encodedText elt.Title ]
                        td [] [ encodedText (TagHelpers.Display.For elt.ReleaseDate) ]
                        td [] [ encodedText elt.Genre ]
                        td [] [ encodedText (TagHelpers.Display.For elt.Price) ]
                        td [] [ encodedText elt.Rating ]

                        td [] [
                            a [ _href (Urls.movies_edit.href elt.Id) ] [ encodedText "Edit" ]
                            encodedText " | "
                            a [ _href (Urls.movies_details.href elt.Id) ] [ encodedText "Details" ]
                            encodedText " | "
                            a [ _href (Urls.movies_delete.href elt.Id) ] [ encodedText "Delete" ]
                        ]
                    ])))
            ]
        ] |> layout "Index" []
    
    let details (model : Movie) = 
        [

            h1 [] [ encodedText "Details" ]

            div [] [

                h4 [] [ encodedText "Movie" ]

                hr []

                dl [ _class "row" ] (

                    let entry (label_str : string) (value_str : string) =
                        [
                            dt [ _class "col-sm-2" ] [ encodedText label_str ]
                            dd [ _class "col-sm-10" ] [ encodedText value_str ]
                        ]                                

                    (entry "Title" model.Title) @ 
                    (entry "Release Date" (string model.ReleaseDate)) @
                    (entry "Genre" model.Genre) @
                    (entry "Price" (string model.Price)) @
                    (entry "Rating" model.Rating)
                )
            ]

            div [] [
                a [ _href (Urls.movies_edit.href model.Id) ] [ encodedText "Edit" ]

                encodedText "|"

                a [ _href "/Movies" ] [ encodedText "Back to list" ]
            ]
        ] |> layout "Details" []

    let create (request_token : string) =
        [
            h1 [] [ encodedText "Create" ]

            h4 [] [ encodedText "Movie" ]

            hr []

            div [ _class "row" ] [
                div [ _class "col-md-4" ] [
                    form [ _action "/Movies/Create"; _method "post" ] [

                        let form_group (expr : FSharp.Quotations.Expr<'a>) =
                            div [ _class "form-group" ] 
                                [
                                    TagHelpers.Label.Of(%expr, [ _class "control-label" ])

                                    TagHelpers.Input.Of(%expr, [ _class "form-control" ])

                                    TagHelpers.SpanValidation.Of(%expr, [ _class "text-danger" ])
                                ]

                        form_group <@ Unchecked.defaultof<Movie>.Title @>
                        form_group <@ Unchecked.defaultof<Movie>.ReleaseDate @>
                        form_group <@ Unchecked.defaultof<Movie>.Genre @>
                        form_group <@ Unchecked.defaultof<Movie>.Price @>
                        form_group <@ Unchecked.defaultof<Movie>.Rating @>

                        div [ _class "form-group" ] [
                            input [ _type "submit"; _value "Create"; _class "btn btn-primary" ]
                        ]

                        input [ 
                            _name "__RequestVerificationToken"
                            _type "hidden"
                            _value request_token
                        ]
                    ]
                ]
            ]
        
            div [] [ a [ _href "/Movies" ] [ encodedText "Back to List" ] ]

        ] |> layout "Create" validation_scripts_partial

    let edit (model : Movie) (request_token : string) =
        [
            h1 [] [ encodedText "Edit" ]

            h4 [] [ encodedText "Movie" ]

            hr []

            div [ _class "row" ] [
                div [ _class "col-md-4" ] [
                    form [ _action (Urls.movies_edit.href model.Id); _method "post" ] [

                        TagHelpers.Input.Of(model.Id, [ _type "hidden" ])

                        let form_group (expr : FSharp.Quotations.Expr<'a>) =
                            div [ _class "form-group" ] 
                                [
                                    TagHelpers.Label.Of(%expr, [ _class "control-label" ])

                                    TagHelpers.Input.Of(%expr, [ _class "form-control" ])

                                    TagHelpers.SpanValidation.Of(%expr, [ _class "text-danger" ])
                                ]

                        form_group <@ model.Title @>
                        form_group <@ model.ReleaseDate @>
                        form_group <@ model.Genre @>
                        form_group <@ model.Price @>
                        form_group <@ model.Rating @>

                        div [ _class "form-group" ] [
                            input [ _type "submit"; _value "Save"; _class "btn btn-primary" ]
                        ]

                        input [
                            _name "__RequestVerificationToken"
                            _type "hidden"
                            _value request_token
                        ]
                    ]
                ]
            ]

            div [] [
                a [ _href "/Movies" ] [ encodedText "Back to List" ]
            ]
        ] |> layout "Edit" validation_scripts_partial

    let delete (model : Movie) (request_token : string) =
        [
            h1 [] [ encodedText "Delete" ]

            h3 [] [ encodedText "Are you sure you want to delete this?" ]

            div [] [
                h4 [] [ encodedText "Movie" ]
                hr []
                
                dl [ _class "row" ] (
                    
                    let entry (label_str : string) (value_str : string) =
                        [
                            dt [ _class "col-sm-2" ] [ encodedText label_str ]
                            dd [ _class "col-sm-10" ] [ encodedText value_str ]
                        ]

                    (entry "Title" model.Title) @ 
                    (entry "Release Date" (string model.ReleaseDate)) @
                    (entry "Genre" model.Genre) @
                    (entry "Price" (string model.Price)) @
                    (entry "Rating" model.Rating)
                )

                form [ _action (Urls.movies_delete.href model.Id); _method "post" ] [

                    TagHelpers.Input.Of(model.Id, [ _type "hidden" ])
                    
                    input [ _type "submit"; _value "Delete"; _class "btn btn-danger" ]

                    encodedText " | "

                    a [ _href "/Movies" ] [ encodedText "Back to List" ]

                    input [ 
                        _name "__RequestVerificationToken"
                        _type "hidden"
                        _value request_token
                    ]
                ]
            ]
        ] |> layout "Delete" []

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

        let antiforgery = ctx.RequestServices.GetService(typeof<Microsoft.AspNetCore.Antiforgery.IAntiforgery>) :?> Microsoft.AspNetCore.Antiforgery.IAntiforgery
        
        let token_set = antiforgery.GetAndStoreTokens(ctx)

        let view = Views.create token_set.RequestToken

        htmlView view next ctx

let post_create_handler : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->

        let antiforgery = ctx.RequestServices.GetService(typeof<Microsoft.AspNetCore.Antiforgery.IAntiforgery>) :?> Microsoft.AspNetCore.Antiforgery.IAntiforgery
                
        task {
            let! result = Async.AwaitTask(antiforgery.IsRequestValidAsync(ctx))

            if result then

                let! movie = ctx.BindFormAsync<Movie>()

                let context = ctx.RequestServices.GetService(typeof<MvcMovieContext>) :?> MvcMovieContext
                
                context.Add(movie) |> ignore
                context.SaveChanges() |> ignore
                
                return! redirectTo false "/Movies" next ctx
                                
            else
                return! RequestErrors.BAD_REQUEST 10 next ctx            
        }
        
let edit_handler (id : int) : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->

        let context = ctx.RequestServices.GetService(typeof<MvcMovieContext>) :?> MvcMovieContext

        let movie = context.Movie.Find(id)

        if (isNull (box movie)) then
            RequestErrors.notFound (text "Not Found") next ctx
        else
            let antiforgery = ctx.RequestServices.GetService(typeof<Microsoft.AspNetCore.Antiforgery.IAntiforgery>) :?> Microsoft.AspNetCore.Antiforgery.IAntiforgery
        
            let token_set = antiforgery.GetAndStoreTokens(ctx)

            let view = Views.edit movie token_set.RequestToken

            htmlView view next ctx

let post_edit_handler (id : int) : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->

        let antiforgery = ctx.RequestServices.GetService(typeof<Microsoft.AspNetCore.Antiforgery.IAntiforgery>) :?> Microsoft.AspNetCore.Antiforgery.IAntiforgery

        task {

            let! result = Async.AwaitTask(antiforgery.IsRequestValidAsync(ctx))

            if result then

                let! movie = ctx.BindFormAsync<Movie>()

                if id <> movie.Id then
                    return! RequestErrors.NOT_FOUND movie next ctx
                else
                    let context = ctx.RequestServices.GetService(typeof<MvcMovieContext>) :?> MvcMovieContext

                    try
                        context.Update(movie) |> ignore
                        context.SaveChanges() |> ignore
                        
                        return! redirectTo false "/Movies" next ctx
                    with
                        | :? DbUpdateConcurrencyException as ex ->
                        return! RequestErrors.NOT_FOUND movie next ctx
            else
                return! RequestErrors.BAD_REQUEST 10 next ctx            
        }

let delete_handler (id : int) : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        
        let context = ctx.RequestServices.GetService(typeof<MvcMovieContext>) :?> MvcMovieContext

        let movie = context.Movie.FirstOrDefault(fun elt -> elt.Id = id)

        if (isNull (box movie)) then
            RequestErrors.notFound (text "delete - Not Found") next ctx
        else
            let antiforgery = ctx.RequestServices.GetService(typeof<Microsoft.AspNetCore.Antiforgery.IAntiforgery>) :?> Microsoft.AspNetCore.Antiforgery.IAntiforgery
        
            let token_set = antiforgery.GetAndStoreTokens(ctx)            

            let view = Views.delete movie token_set.RequestToken

            htmlView view next ctx           

let post_delete_handler (id : int) : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->

        let antiforgery = ctx.RequestServices.GetService(typeof<Microsoft.AspNetCore.Antiforgery.IAntiforgery>) :?> Microsoft.AspNetCore.Antiforgery.IAntiforgery    

        task {
            let! result = Async.AwaitTask(antiforgery.IsRequestValidAsync(ctx))

            if result then

                let context = ctx.RequestServices.GetService(typeof<MvcMovieContext>) :?> MvcMovieContext

                let movie = context.Movie.Find(id)

                context.Movie.Remove(movie) |> ignore
                context.SaveChanges()       |> ignore
                
                return! redirectTo false "/Movies" next ctx
            else
                return! RequestErrors.BAD_REQUEST 10 next ctx
        }
       
let webApp =

    choose [
        GET >=>
            choose [
                route  Urls.movies               >=> movies_handler
                route  Urls.movies_create        >=> create_handler                
                routef Urls.movies_details.route     details_handler
                routef Urls.movies_edit.route        edit_handler
                routef Urls.movies_delete.route      delete_handler
            ]
                
        POST >=> choose [ 
            route  Urls.movies_create       >=> post_create_handler
            routef Urls.movies_edit.route       post_edit_handler
            routef Urls.movies_delete.route     post_delete_handler
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
    
    services.AddAntiforgery() |> ignore

    services.AddDbContext<MvcMovieContext>(fun options ->
            // options.UseSqlite(Configuration.GetConnectionString("MvcMovieContext"))
            options.UseSqlite("Data Source=MvcMovie.db") |> ignore
            // options.UseInMemoryDatabase("DB_ToDo") |> ignore
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