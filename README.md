
# Introduction

The ASP.NET Core documentation includes an MVC tutorial for building a project called [MvcMovies](https://docs.microsoft.com/en-us/aspnet/core/tutorials/first-mvc-app/?view=aspnetcore-5.0).

This is a port of that project to F# and [Giraffe](https://github.com/giraffe-fsharp/Giraffe).

Here's what it looks like:

<img src="https://i.imgur.com/NGqH1H7.png" width="500">

# Setup

```
git clone https://github.com/dharmatech/mvc-movie-giraffe.git
cd mvc-movie-giraffe
libman restore
dotnet restore
dotnet watch run
```

Then go to:

    http://localhost:5000/Movies

# Thanks

Chet Husk [demonstrated](https://github.com/giraffe-fsharp/Giraffe/discussions/457#discussioncomment-206261) a very nice expression-based approach to tag-helpers.
