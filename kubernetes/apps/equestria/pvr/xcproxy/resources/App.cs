#!/Users/david/.local/share/mise/installs/1password-cli/2.30.3/bin/op run --no-masking -- dotnet run
// #:package YamlDotNet@16.3.0
#:sdk Microsoft.NET.Sdk.Web
#:package Spectre.Console@0.50.0
#:package Spectre.Console.Json@0.50.0
#:package Dumpify@0.6.6
#:package Lunet.Extensions.Logging.SpectreConsole@1.2.0
#:package System.Text.Json@8.0.0
#:package System.Text.RegularExpressions@4.3.1
#:package Xtream.Client@1.0.7
#:package ZiggyCreatures.FusionCache@2.5.0
#:package NeoSmart.Caching.Sqlite.AspNetCore@9.0.1
#:package ZiggyCreatures.FusionCache.Serialization.SystemTextJson@2.5.0
#:package TMDbLib@3.0.0
#:package System.Reactive@7.0.0-preview.1
#:package System.Linq.Async@7.0.0
#:property JsonSerializerIsReflectionEnabledByDefault=true

using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using System.Windows.Markup;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using NeoSmart.Caching.Sqlite;
using Spectre.Console;
using TMDbLib.Client;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.Search;
using TMDbLib.Objects.TvShows;
using Xtream.Client;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;
var builder = WebApplication.CreateBuilder(args);

// Configuration
var config = XcProxyConfiguration.LoadFromEnvironment(builder.Configuration);
builder.Services.AddSingleton(config);
builder.Services.AddSingleton<TmdbEnricher>();
builder.Services.AddSingleton<M3uParser>();
builder.Services.AddSingleton<PlaylistData>();
builder.Services.AddSingleton(sp => new TMDbClient(config.TmdbApiKey));
builder.Services.AddMemoryCache();
builder.Services.AddOutputCache();
builder.Services.AddSqliteCache(o =>
{
  o.CachePath = config.MetaCacheFile;
  o.CleanupInterval = TimeSpan.FromHours(1);
});
builder.Services.AddFusionCache()
.WithoutBackplane()
.WithRegisteredMemoryCache()
.WithRegisteredDistributedCache()
.WithLogger(NullLogger<FusionCache>.Instance)
.WithSystemTextJsonSerializer();
builder.Services.AddHttpClient("tmdb");
builder.Services.AddHttpClient();

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
var m3uParser = app.Services.GetRequiredService<M3uParser>();
var tmdbClient = app.Services.GetRequiredService<TmdbEnricher>();

// ============================================
// HELPER FUNCTIONS
// ============================================


Func<XcProxyConfiguration, ServerInfo> CreateServerInfo = (config) =>
{
  return new ServerInfo(
      Url: "xcproxy.${CLUSTER_DOMAIN}",
      Port: "443",
      HttpsPort: "443",
      ServerProtocol: "https",
      RtmpPort: "0",
      Timezone: "UTC",
      TimestampNow: DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
      TimeNow: DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
      Process: true,
      ApiVersion: 2,
      AllowedOutputFormats: ["m3u8", "ts", "mp4", "mkv"]
  );
};

static string? PosterPath(string? path)
{
  if (string.IsNullOrEmpty(path))
    return null;
  return $"https://image.tmdb.org/t/p/w500{path}";
}

static string? BackdropPath(string? path)
{
  if (string.IsNullOrEmpty(path))
    return null;
  return $"https://image.tmdb.org/t/p/w780{path}";
}

string? PickTvPoster(string? feedLogo, TvShow? md)
{
  if (!string.IsNullOrEmpty(md?.PosterPath))
    return PosterPath(md.PosterPath)!;
  if (!string.IsNullOrEmpty(feedLogo))
    return feedLogo;
  return null;
}
string? PickTvBackdrop(string? feedLogo, TvShow? md)
{
  if (!string.IsNullOrEmpty(md?.BackdropPath))
    return BackdropPath(md.BackdropPath)!;
  if (!string.IsNullOrEmpty(feedLogo))
    return feedLogo;
  return null;
}
int GetTvCategoryId(int defaultCatId, TvShow? md)
{
  if (md is { GenreIds: { Count: > 0 } genreIds })
  {
    return genreIds[0];
  }
  return config.SeriesCategoryId;
}
string GetPlot(string? overview)
{
  var plot = overview ?? "";
  plot = plot[..Math.Min(plot.Length, 1000)]; // Trim to 1000 chars
  return plot;
}
string? PickMoviePoster(string? feedLogo, Movie? md)
{
  if (!string.IsNullOrEmpty(md?.PosterPath))
    return PosterPath(md.PosterPath)!;
  if (!string.IsNullOrEmpty(feedLogo))
    return feedLogo;
  return null;
}
string? PickMovieBackdrop(string? feedLogo, Movie? md)
{
  if (!string.IsNullOrEmpty(md?.BackdropPath))
    return BackdropPath(md.BackdropPath)!;
  if (!string.IsNullOrEmpty(feedLogo))
    return feedLogo;
  return null;
}
int GetMovieCategoryId(string defaultCatId, Movie? md)
{
  if (md is { Genres: { Count: > 0 } genreIds })
  {
    return genreIds[0].Id;
  }
  return config.MovieCategoryId;
}


// ============================================
// ENDPOINTS
// ============================================

app.MapGet("/health", async ([FromServices] PlaylistData playlistData) =>
{
  var movies = await playlistData.LoadMoviesAsync();
  var series = await playlistData.LoadSeriesAsync();
  return Results.Ok(new
  {
    ok = true,
    movies = movies.Count,
    episodes = series.SelectMany(z => z.Seasons).SelectMany(z => z.Value).Count(),
    series = series.Count,
    time = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
  });
});

app.MapGet("/", () => Results.Ok("OK"));

app.MapGet("/panel_api.php", ([FromServices] XcProxyConfiguration cfg) =>
{
  var userInfo = new UserInfo(1, "Active", cfg.Username, cfg.Password, 0, 1, 0, "2147483647", "");
  var serverInfo = CreateServerInfo(cfg);
  return Results.Ok(new { user_info = userInfo, server_info = serverInfo });
});

app.MapGet("/player_api.php", async (string? action, string? category_id, int? vod_id, int? series_id, int? limit, HttpContext ctx) =>
{
  var playlistData = app.Services.GetRequiredService<PlaylistData>();
  var cfg = app.Services.GetRequiredService<XcProxyConfiguration>();
  var cache = app.Services.GetRequiredService<IFusionCache>();

  // Local action handlers

  async Task<Ok<IEnumerable<XtreamCategory>>> HandleGetVodCategories()
  {
    if (await cache.TryGetAsync<List<XtreamCategory>>("movies_categories") is { HasValue: true, Value: var cached })
    {
      return TypedResults.Ok(cached.AsEnumerable());
    }

    var movies = await playlistData.LoadMoviesAsync();
    var results = await movies.ToObservable()
    .Select((it, index) => tmdbClient.SearchMovieAsync(it.Title, it.Year).ToObservable())
    .Merge(3)
    .SelectMany(z => z?.Genres ?? [])
    .Select(z => new XtreamCategory(z.Id, z.Name ?? "Unknown", "0"))
    .Distinct(z => z.CategoryId)
    .StartWith(new XtreamCategory(cfg.MovieCategoryId, "Uncategorized", "0"))
    .ToAsyncEnumerable()
    .OrderBy(z => z.CategoryName)
    .ToListAsync();

    cache.Set("movies_categories", results, TimeSpan.FromHours(1));

    return TypedResults.Ok(results.AsEnumerable());
  }

  async Task<Ok<IEnumerable<XtreamCategory>>> HandleGetSeriesCategories()
  {
    if (await cache.TryGetAsync<List<XtreamCategory>>("series_categories") is { HasValue: true, Value: var cached })
    {
      return TypedResults.Ok(cached.AsEnumerable());
    }

    var series = await playlistData.LoadSeriesAsync();
    var results = await series.ToObservable()
    .Select((it, index) => tmdbClient.SearchSeriesAsync(it.Info.SeriesName).ToObservable())
    .Merge(3)
    .SelectMany(z => z?.Genres ?? [])
    .Select(z => new XtreamCategory(z.Id, z.Name ?? "Unknown", "0"))
    .Distinct(z => z.CategoryId)
    .StartWith(new XtreamCategory(cfg.SeriesCategoryId, "Uncategorized", "0"))
    .ToAsyncEnumerable()
    .OrderBy(z => z.CategoryName)
    .ToListAsync();

    cache.Set("series_categories", results, TimeSpan.FromHours(1));

    return TypedResults.Ok(results.AsEnumerable());
  }

  async Task<Ok<IEnumerable<XtreamVodStream>>> HandleGetVodStreams()
  {
    var movies = await playlistData.LoadMoviesAsync();
    IEnumerable<MovieItem> m = limit.HasValue && limit > 0 ? movies.Take(limit.Value).ToList() : movies;

    var results = await m.ToObservable()
    .Select((it, index) => GetVodStream(tmdbClient, cfg, it, index).ToObservable())
    .Merge(3)
    .ToAsyncEnumerable()
    .ToListAsync();

    return TypedResults.Ok(results.AsEnumerable());
  }

  async Task<XtreamVodStream> GetVodStream(TmdbEnricher tmdbClient, XcProxyConfiguration cfg, MovieItem it, int index)
  {
    var md = await tmdbClient.SearchMovieAsync(it.Title, it.Year);

    var posterFinal = PickMoviePoster(it.StreamIcon, md);
    var backdropFinal = PickMovieBackdrop(it.StreamIcon, md);
    var plot = GetPlot(md?.Overview);

    var catId = cfg.MovieCategoryId;
    if (md is { Genres: { Count: > 0 } genreIds })
    {
      catId = genreIds[0].Id;
    }

    return new XtreamVodStream(
      it.StreamId,
      md?.Title ?? it.Title,
      "movie",
      it.StreamId,
      posterFinal ?? backdropFinal ?? "",
      md?.VoteAverage.ToString() ?? "0.0",
      (md?.ReleaseDate ?? DateTimeOffset.Now).ToUnixTimeSeconds(),
      catId,
      it.ContainerExtension,
"",
"",
      posterFinal ?? "",
      md?.Id.ToString() ?? "",
      backdropFinal is { } ? [backdropFinal] : [],
      "",
       string.Join(", ", md?.Genres?.Select(g => g.Name) ?? []),
      GetPlot(md?.Overview),
      string.Join(", ", md?.Credits?.Cast?.Select(z => z.Name) ?? []),
      md?.VoteAverage.ToString() ?? "0.0",
      md?.ReleaseDate?.ToString("yyyy-MM-dd") ?? DateTimeOffset.Now.ToUnixTimeSeconds().ToString(),
      (md?.Runtime ?? 0) * 60,
      TimeSpan.FromMinutes(md?.Runtime ?? 0).ToString(@"hh\:mm\:ss")
    );
  }

  async Task<IResult> HandleGetVodInfo(int vodId)
  {
    var movies = await playlistData.LoadMoviesAsync();
    var it = movies.FirstOrDefault(x => x.StreamId == vodId);
    if (it == null)
      return Results.NotFound(new { error = "not found" });

    var item = await GetVodStream(tmdbClient, cfg, it, 0);

    var info = new XtreamVodDetail(
  item,
      new XtreamVodData(
      it.StreamId,
      item.Name,
      item.Added,
      item.CategoryId,
      it.ContainerExtension
      )
    );
    return Results.Ok(info);
  }

  async Task<IResult> HandleGetSeries()
  {
    var series = await playlistData.LoadSeriesAsync();
    IEnumerable<SeriesItem> src = series;
    if (limit.HasValue && limit > 0)
      src = src.Take(limit.Value).ToList();

    var results = await src.ToObservable()
    .Select((it) => GetSeriesItemAsync(tmdbClient, cfg, it).ToObservable())
    .Merge(3)
    .ToAsyncEnumerable()
    .ToListAsync();

    return Results.Ok(results);
  }


  async Task<XtreamSeriesStream> GetSeriesItemAsync(TmdbEnricher tmdbClient, XcProxyConfiguration cfg, SeriesItem s)
  {
    var seriesTitle = s.Info.SeriesName;
    var md = await tmdbClient.SearchSeriesAsync(seriesTitle);

    var posterFinal = PickTvPoster(s.Info.Poster, md);
    var backdropFinal = PickTvBackdrop(s.Info.Poster, md);
    var catId = GetTvCategoryId(cfg.SeriesCategoryId, md);

    var seasonCount = s.Seasons.Count;
    var episodeCount = s.Seasons.Values.Sum(v => v.Count);

    return new XtreamSeriesStream(
      s.Info.SeriesId,
      md?.Name ?? seriesTitle,
      s.Info.SeriesId,
      catId,
      posterFinal ?? "",
      GetPlot(md?.Overview),
string.Join(", ", md?.Credits?.Cast?.Select(z => z.Name) ?? []),
"",
string.Join(", ", md?.Genres?.Select(g => g.Name) ?? []),
          md?.FirstAirDate?.ToString("yyyy-MM-dd") ?? DateTimeOffset.Now.ToUnixTimeSeconds().ToString(),
          DateTimeOffset.Now.ToUnixTimeSeconds().ToString(),

      md?.VoteAverage.ToString() ?? "0.0",
      (md?.VoteAverage ?? 0.0) / 2.0,
backdropFinal is { } ? [backdropFinal] : [],
      "",
        (md?.EpisodeRunTime ?? []).FirstOrDefault().ToString()
    );
  }

  async Task<IResult> HandleGetSeriesInfo(int seriesId)
  {
    var series = await playlistData.LoadSeriesAsync();
    var s = series.FirstOrDefault(x => x.Info.SeriesId == seriesId);
    if (s == null)
      return Results.NotFound(new { error = "not found" });

    var streamInfo = await GetSeriesItemAsync(tmdbClient, cfg, s);
    var md = await tmdbClient.SearchSeriesAsync(streamInfo.Name);

    var episodesOut = new Dictionary<string, IReadOnlyList<XtreamSeriesEpisode>>();
    var seasons = new List<XtreamSeriesSeason>();

    foreach (var (season, epList) in s.Seasons.OrderBy(x => x.Key))
    {
      var tmdbSeason = md?.Seasons?.Find(x => x.SeasonNumber == season);
      seasons.Add(new XtreamSeriesSeason(
  tmdbSeason?.AirDate?.ToString("yyyy-MM-dd") ?? "",
  tmdbSeason?.EpisodeCount ?? epList.Count,
  tmdbSeason?.Id ?? season,
  tmdbSeason?.Name ?? $"Season {season}",
  GetPlot(tmdbSeason?.Overview),
  tmdbSeason?.SeasonNumber ?? season,
  PosterPath(tmdbSeason?.PosterPath) ?? streamInfo.Cover ?? "",
  PosterPath(tmdbSeason?.PosterPath) ?? streamInfo.Cover ?? ""
      ));
      var epInfos = new List<XtreamSeriesEpisode>();

      var episodeData = (md?.EpisodeGroups?.Results?
      .SelectMany(z => z.Groups ?? [])
        .SelectMany(z => z.Episodes ?? []) ?? []
  ).ToFrozenSet();

      foreach (var ep in epList.OrderBy(x => x.Episode))
      {
        var tmdbEpisode = episodeData.FirstOrDefault(z => z.SeasonNumber == ep.Season && z.EpisodeNumber == ep.Episode);
        epInfos.Add(new XtreamSeriesEpisode(
          ep.Id,
          tmdbEpisode?.EpisodeNumber ?? ep.Episode,
          tmdbEpisode?.Name ?? ep.Title,
  ep.ContainerExtension,
  new XtreamSeriesEpisodeInfo(
  PosterPath(tmdbSeason?.PosterPath ?? tmdbEpisode?.StillPath) ?? streamInfo.Cover ?? "",
  GetPlot(tmdbEpisode?.Overview),
  tmdbEpisode?.AirDate?.ToString("yyyy-MM-dd") ?? "",
  tmdbEpisode?.VoteAverage.ToString() ?? "0.0",
        (tmdbEpisode?.Runtime ?? 0) * 60,
        TimeSpan.FromMinutes(tmdbEpisode?.Runtime ?? 0).ToString(@"hh\:mm\:ss")
  )));
      }

      episodesOut[$"{season}"] = epInfos;
    }


    var info = new XtreamSeriesDetail(episodesOut, streamInfo, seasons);

    return Results.Ok(info);
  }

  return action switch
  {
    "get_live_categories" => Results.Ok(Array.Empty<string>().ToAsyncEnumerable()),
    "get_live_streams" => Results.Ok(Array.Empty<string>().ToAsyncEnumerable()),
    "get_vod_categories" => await HandleGetVodCategories(),
    "get_series_categories" => await HandleGetSeriesCategories(),
    "get_vod_streams" => await HandleGetVodStreams(),
    "get_vod_info" => await HandleGetVodInfo(vod_id ?? 0),
    "get_series" => await HandleGetSeries(),
    "get_series_info" => await HandleGetSeriesInfo(series_id ?? 0),
    _ => GetDefaultResponse()
  };

  IResult GetDefaultResponse()
  {
    var userInfo = new UserInfo(1, "Active", cfg.Username, cfg.Password, 0, 1, 0, "2147483647", "");
    var serverInfo = CreateServerInfo(cfg);
    return Results.Json(new { user_info = userInfo, server_info = serverInfo });
  }
});

app.MapGet("/xmltv.php", async ([FromServices] PlaylistData playlistData) =>
{
  var movies = await playlistData.LoadMoviesAsync();
  var series = await playlistData.LoadSeriesAsync();
  var cfg = app.Services.GetRequiredService<IOptions<XcProxyConfiguration>>().Value;

  var sb = new StringBuilder();
  sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
  sb.AppendLine("<tv>");

  foreach (var it in movies)
  {
    sb.AppendLine($"  <channel id=\"movie-{it.StreamId}\"><display-name>{System.Net.WebUtility.HtmlEncode(it.Title)}</display-name></channel>");
  }

  foreach (var s in series)
  {
    sb.AppendLine($"  <channel id=\"series-{s.Info.SeriesId}\"><display-name>{System.Net.WebUtility.HtmlEncode(s.Info.SeriesName)}</display-name></channel>");
  }

  sb.AppendLine("</tv>");

  return Results.Content(sb.ToString(), "application/xml");
});

app.MapMethods("/xmltv.php", new[] { "HEAD" }, () =>
{
  return Results.Ok();
});

app.MapMethods("/movie/{username}/{password}/{stream_id}.{ext}", new[] { "HEAD" }, (string username, string password, int stream_id) =>
{
  return Results.Ok();
});

app.MapGet("/movie/{username}/{password}/{stream_id}.{ext}", async (string username, string password, int stream_id, [FromServices] PlaylistData playlistData) =>
{
  var movies = await playlistData.LoadMoviesAsync();
  var it = movies.FirstOrDefault(x => x.StreamId == stream_id);
  if (it == null)
    return Results.NotFound();

  // Redirect to direct source
  return Results.Redirect(it.DirectSource);
});

app.MapMethods("/series/{username}/{password}/{episode_id}.{ext}", new[] { "HEAD" }, (string username, string password, int episode_id) =>
{
  return Results.Ok();
});

app.MapGet("/series/{username}/{password}/{episode_id}.{ext}", async (string username, string password, int episode_id, [FromServices] PlaylistData playlistData) =>
{
  var episodes = await playlistData.LoadEpisodesAsync();
  var ep = episodes.GetValueOrDefault(episode_id);
  if (ep == null)
    return Results.NotFound();

  // Redirect to direct source
  return Results.Redirect(ep.DirectSource);
});

app.MapGet("/get.php", async ([FromServices] PlaylistData playlistData) =>
{
  var movies = await playlistData.LoadMoviesAsync();
  var series = await playlistData.LoadSeriesAsync();
  var host = "https://xcproxy.${CLUSTER_DOMAIN}";
  var lines = new List<string> { "#EXTM3U" };

  // Movie entries
  foreach (var it in movies)
  {
    var name = it.Title;
    var url = $"{host}/movie/user/pass/{it.StreamId}.{it.ContainerExtension}";
    lines.Add($"#EXTINF:-1 tvg-id=\"\" tvg-name=\"{name}\" tvg-logo=\"{it.StreamIcon}\" group-title=\"Movie VOD\",{name}");
    lines.Add(url);
  }

  // Series entries
  foreach (var s in series)
  {
    foreach (var season in s.Seasons)
    {
      var seriesName = s.Info?.SeriesName ?? "Series";
      foreach (var ep in season.Value)
      {
        var name = $"{seriesName} - {ep.Title}";
        var url = $"{host}/series/user/pass/{ep.Id}.{ep.ContainerExtension}";
        lines.Add($"#EXTINF:-1 tvg-id=\"\" tvg-name=\"{name}\" tvg-logo=\"{ep.Poster}\" group-title=\"{seriesName}\",{name}");
        lines.Add(url);
      }
    }
  }

  var content = string.Join("\n", lines);
  return Results.Content(content + "\n", "audio/x-mpegurl");
});

await app.RunAsync();

// ============================================
// CONFIGURATION
// ============================================

public record XcProxyConfiguration
{
  public required string Version { get; init; }
  public required string MovieM3uUrl { get; init; }
  public required string SeriesM3uUrl { get; init; }
  public required string M3uUrlFallback { get; init; }
  public required string Username { get; init; }
  public required string Password { get; init; }
  public required int CacheTtlSeconds { get; init; }
  public required int MovieCategoryId { get; init; }
  public required int SeriesCategoryId { get; init; }
  public required string TmdbApiKey { get; init; }
  public required string TmdbLanguage { get; init; }
  public required string TmdbImageBase { get; init; }
  public required string TmdbImageBackdrop { get; init; }
  public required bool EnrichDetails { get; init; }
  public required string MetaCacheFile { get; init; }
  public required string StreamMode { get; init; } // "redirect" | "proxy"
  public required int StreamChunkSize { get; init; }
  public required bool CategoryPickFirst { get; init; }
  public required bool TmdbOnList { get; init; }
  public required IReadOnlyList<string> PreferredQualities { get; init; }
  public required IReadOnlyList<string> PreferredSources { get; init; }

  public static XcProxyConfiguration LoadFromEnvironment(IConfiguration envConfig)
  {
    return new XcProxyConfiguration
    {
      Version = envConfig["XC_VERSION"] ?? "xcproxy-2025-09-09k14",
      MovieM3uUrl = (envConfig["MOVIE_M3U_URL"] ?? "").Trim(),
      SeriesM3uUrl = (envConfig["SERIES_M3U_URL"] ?? "").Trim(),
      M3uUrlFallback = (envConfig["M3U_URL"] ?? "").Trim(),
      Username = envConfig["XC_USER"] ?? "U",
      Password = envConfig["XC_PASS"] ?? "P",
      CacheTtlSeconds = int.TryParse(envConfig["CACHE_TTL"], out var ttl) ? ttl : 900,
      MovieCategoryId = int.TryParse(envConfig["MOVIE_CAT_ID"], out var movieCatId) ? movieCatId : 42926828,
      SeriesCategoryId = int.TryParse(envConfig["SERIES_CAT_ID"], out var seriesCatId) ? seriesCatId : 42984329,
      TmdbApiKey = (envConfig["TMDB_API_KEY"] ?? "").Trim(),
      TmdbLanguage = envConfig["TMDB_LANG"] ?? "en-US",
      TmdbImageBase = envConfig["TMDB_IMG_BASE"] ?? "https://image.tmdb.org/t/p/w500",
      TmdbImageBackdrop = envConfig["TMDB_IMG_BACKDROP"] ?? "https://image.tmdb.org/t/p/w780",
      EnrichDetails = IsTruthy(envConfig["XC_ENRICH_DETAILS"]),
      MetaCacheFile = envConfig["META_CACHE_FILE"] ?? "/cache/tmdb_cache.json",
      StreamMode = (envConfig["STREAM_MODE"] ?? "redirect").ToLowerInvariant(),
      StreamChunkSize = int.TryParse(envConfig["STREAM_CHUNK"], out var chunk) ? chunk : 65536,
      CategoryPickFirst = IsTruthy(envConfig["CATEGORY_PICK_FIRST"] ?? "true"),
      TmdbOnList = IsTruthy(envConfig["TMDB_ON_LIST"]),
      PreferredQualities = ParsePreferenceList(envConfig["PREF_QUALITY"], "2160p,1080p,720p,480p"),
      PreferredSources = ParsePreferenceList(envConfig["PREF_SOURCE"], "remux,bluray,web,webrip,other"),
    };

    static bool IsTruthy(string? value) =>
        !string.IsNullOrEmpty(value) && (
        value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("on", StringComparison.OrdinalIgnoreCase));

    static IReadOnlyList<string> ParsePreferenceList(string? value, string defaults)
    {
      var s = (value ?? defaults ?? "").Trim();
      return string.IsNullOrEmpty(s)
          ? []
          : s.Split(',')
              .Select(x => x.Trim().ToLowerInvariant())
              .Where(x => !string.IsNullOrEmpty(x))
              .ToList()
              .AsReadOnly();
    }
  }
}

// ============================================
// DATA MODELS
// ============================================

public class PlaylistData(M3uParser m3uParser, IFusionCache cache, XcProxyConfiguration config)
{

  private FrozenSet<MovieItem>? movieItems;
  private FrozenSet<SeriesItem>? seriesItems;
  private FrozenDictionary<int, EpisodeItem>? episodeItems;

  public async Task<FrozenSet<MovieItem>> LoadMoviesAsync()
  {
    return movieItems ??= (await cache.GetOrSetAsync("movies", ct => LoadMoviesInternalAsync(), TimeSpan.FromHours(3))).ToFrozenSet();
  }

  public async Task<FrozenSet<SeriesItem>> LoadSeriesAsync()
  {
    return seriesItems ??= (await cache.GetOrSetAsync("series", ct => LoadSeriesInternalAsync(), TimeSpan.FromHours(3))).ToFrozenSet();
  }

  public async Task<FrozenDictionary<int, EpisodeItem>> LoadEpisodesAsync()
  {
    var series = await LoadSeriesAsync();
    return episodeItems ??= series.SelectMany(s => s.Seasons.Values).SelectMany(eps => eps).ToFrozenDictionary(z => z.Id);
  }

  async Task<HashSet<MovieItem>> LoadMoviesInternalAsync()
  {
    using var client = new HttpClient();
    var result = await m3uParser.ParseMovies(await client.GetStreamAsync(config.MovieM3uUrl)).ToListAsync();
    return result.ToHashSet();
  }

  async Task<HashSet<SeriesItem>> LoadSeriesInternalAsync()
  {
    using var client = new HttpClient();
    var results = new List<SeriesItem>();
    var set = (await m3uParser.ParseSeries(await client.GetStreamAsync(config.SeriesM3uUrl)).ToListAsync()).ToHashSet();
    foreach (var item in set.GroupBy(z => z.series.SeriesId))
    {
      var seasons = item
      .GroupBy(z => z.episode.Season)
        .ToDictionary(
          z => z.Key,
          z => z.Select(z => z.episode).ToList() as IReadOnlyList<EpisodeItem>);
      results.Add(new SeriesItem(item.First().series, seasons));
    }
    return results.ToHashSet();
  }

}

public record MovieItem(
    int Number,
    int StreamId,
    string Title,
    string? StreamIcon,
    int? Year,
    string CategoryId,
    string ContainerExtension,
    string DirectSource);

public record EpisodeItem(
    int Id,
    string Title,
    int Season,
    int Episode,
    int SeriesId,
    string? Poster,
    string ContainerExtension,
    string DirectSource);

public record SeriesInfo(
    int SeriesId,
    string SeriesName,
    string? Poster);

public record SeriesItem(
    SeriesInfo Info,
    IReadOnlyDictionary<int, IReadOnlyList<EpisodeItem>> Seasons);

public record XtreamCategory(
    [property: JsonPropertyName("category_id")] int CategoryId,
    [property: JsonPropertyName("category_name")] string CategoryName,
    [property: JsonPropertyName("parent_id")] string ParentId);

public record XtreamVodStream(
    [property: JsonPropertyName("num")] int Num,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("stream_type")] string StreamType,
    [property: JsonPropertyName("stream_id")] int StreamId,
    [property: JsonPropertyName("stream_icon")] string StreamIcon,
    [property: JsonPropertyName("rating")] string Rating,
    [property: JsonPropertyName("added")] long Added,
    [property: JsonPropertyName("category_id")] int CategoryId,
    [property: JsonPropertyName("container_extension")] string ContainerExtension,
    [property: JsonPropertyName("custom_sid")] string CustomSid,
    [property: JsonPropertyName("direct_source")] string DirectSource,
    [property: JsonPropertyName("movie_image")] string MovieImage,
    [property: JsonPropertyName("tmdb_id")] string TmdbId,
    [property: JsonPropertyName("backdrop_path")] IReadOnlyList<string> BackdropPath,
    [property: JsonPropertyName("youtube_trailer")] string YoutubeTrailer,
    [property: JsonPropertyName("genre")] string Genre,
    [property: JsonPropertyName("plot")] string Plot,
    [property: JsonPropertyName("cast")] string Cast,
    [property: JsonPropertyName("director")] string Director,
    [property: JsonPropertyName("releasedate")] string Releasedate,
    [property: JsonPropertyName("duration_secs")] int DurationSecs,
    [property: JsonPropertyName("duration")] string Duration)
{
  public string Logo => StreamIcon;
  public int Rating5Based => (int)(double.Parse(Rating) / 2.0);
  public string Description => Plot;

}

public record XtreamVodDetail(
    [property: JsonPropertyName("info")] XtreamVodStream Info,
    [property: JsonPropertyName("movie_data")] XtreamVodData MovieData);

public record XtreamVodData(
    [property: JsonPropertyName("stream_id")] int StreamId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("added")] long Added,
    [property: JsonPropertyName("category_id")] int CategoryId,
    [property: JsonPropertyName("container_extension")] string ContainerExtension);

public record XtreamSeriesStream(
    [property: JsonPropertyName("num")] int Num,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("series_id")] int SeriesId,
    [property: JsonPropertyName("category_id")] int CategoryId,
    [property: JsonPropertyName("cover")] string Cover,
    [property: JsonPropertyName("plot")] string Plot,
    [property: JsonPropertyName("cast")] string Cast,
    [property: JsonPropertyName("director")] string Director,
    [property: JsonPropertyName("genre")] string Genre,
    [property: JsonPropertyName("releaseDate")] string ReleaseDate,
    [property: JsonPropertyName("last_modified")] string LastModified,
    [property: JsonPropertyName("rating")] string Rating,
    [property: JsonPropertyName("rating_5based")] double Rating5Based,
    [property: JsonPropertyName("backdrop_path")] IReadOnlyList<string> BackdropPath,
    [property: JsonPropertyName("youtube_trailer")] string YoutubeTrailer,
    [property: JsonPropertyName("episode_run_time")] string EpisodeRunTime)
{
  public string Logo => Cover;
  public string Description => Plot;
}

public record XtreamSeriesDetail(
    [property: JsonPropertyName("episodes")] Dictionary<string, IReadOnlyList<XtreamSeriesEpisode>> Episodes,
    [property: JsonPropertyName("info")] XtreamSeriesStream Info,
    [property: JsonPropertyName("seasons")] IReadOnlyList<XtreamSeriesSeason> Seasons);

public record XtreamSeriesSeason(
  [property: JsonPropertyName("air_date")] string AirDate,
  [property: JsonPropertyName("episode_count")] int EpisodeCount,
  [property: JsonPropertyName("id")] int Id,
  [property: JsonPropertyName("name")] string Name,
  [property: JsonPropertyName("overview")] string Overview,
  [property: JsonPropertyName("season_number")] int SeasonNumber,
  [property: JsonPropertyName("cover")] string Cover,
  [property: JsonPropertyName("cover_big")] string CoverBig
);

public record XtreamSeriesEpisode(
  [property: JsonPropertyName("id")] int Id,
  [property: JsonPropertyName("episode_num")] long EpisodeNum,
  [property: JsonPropertyName("title")] string Title,
  [property: JsonPropertyName("container_extension")] string ContainerExtension,
  [property: JsonPropertyName("info")] XtreamSeriesEpisodeInfo Info
);
public record XtreamSeriesEpisodeInfo(
  [property: JsonPropertyName("movie_image")] string MovieImage,
  [property: JsonPropertyName("plot")] string Plot,
  [property: JsonPropertyName("releasedate")] string ReleaseDate,
  [property: JsonPropertyName("rating")] string Rating,
  [property: JsonPropertyName("duration_secs")] int DurationSecs,
  [property: JsonPropertyName("duration")] string Duration);

public record UserInfo(
    [property: JsonPropertyName("auth")] int Auth,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("password")] string Password,
    [property: JsonPropertyName("active_cons")] int ActiveConnections,
    [property: JsonPropertyName("max_connections")] int MaxConnections,
    [property: JsonPropertyName("is_trial")] int IsTrial,
    [property: JsonPropertyName("exp_date")] string ExpiryDate,
    [property: JsonPropertyName("message")] string Message);

public record ServerInfo(
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("port")] string Port,
    [property: JsonPropertyName("https_port")] string HttpsPort,
    [property: JsonPropertyName("server_protocol")] string ServerProtocol,
    [property: JsonPropertyName("rtmp_port")] string RtmpPort,
    [property: JsonPropertyName("timezone")] string Timezone,
    [property: JsonPropertyName("timestamp_now")] long TimestampNow,
    [property: JsonPropertyName("time_now")] string TimeNow,
    [property: JsonPropertyName("process")] bool Process,
    [property: JsonPropertyName("api_version")] int ApiVersion,
    [property: JsonPropertyName("allowed_output_formats")] IReadOnlyList<string> AllowedOutputFormats);

// ============================================
// REGEX PATTERNS - SOURCE GENERATORS
// ============================================

public static partial class RegexPatterns
{
  [GeneratedRegex(@"(\b19\d{2}\b|\b20\d{2}\b)")]
  public static partial Regex YearRegex();

  [GeneratedRegex(@"(?i)\bS(\d{1,3})E(\d{1,3})\b")]
  public static partial Regex SeasonEpisodeRegex();

  [GeneratedRegex(@"(?i)\bS\s*(\d{1,3})\b")]
  public static partial Regex SeasonOnlyRegex();

  // match and remove the date
  // The Daily Show 2024 04 15 720p WEB H264-JEBAITED"
  // [GeneratedRegex(@"(?i)\b(19\d{2}|20\d{2})[ ._-](0[1-9]|1[0-2])[ ._-](0[1-9]|[12][0-9]|3[01])\b")]

  [GeneratedRegex(@"(?i)\b(\d{4}) (\d{2}) (\d{2})\b")]
  public static partial Regex DailyRegex();

  [GeneratedRegex(@"[\(\[][^)\]]{1,40}[\)\]]")]
  public static partial Regex BracketedRegex();

  [GeneratedRegex(@"(?i)\b(1080p|720p|2160p|4k|webrip|web[- ]?dl|blu[- ]?ray|brrip|bdrip|remux|hdr|hevc|x264|x265|h\.?264|h\.?265|av1|aac|dts|truehd|atmos|extended|unrated|proper|repack|limited|imax|hdr10\+?|dolby|vision|director.?s? ?cut|dual ?audio|multi ?audio|subbed|dubbed)\b")]
  public static partial Regex DustRegex();

  [GeneratedRegex(@"\s{2,}")]
  public static partial Regex MultiSpaceRegex();

  [GeneratedRegex(@"(\w[\w-]*)=""([^""]*)""")]
  public static partial Regex M3uAttributeRegex();

  [GeneratedRegex(@"#EXTINF:-?\d+\s*(?<attrs>[^,]*)\s*,(?<title>.*)$")]
  public static partial Regex ExtinfRegex();

  [GeneratedRegex(@"^"".*\btvg-")]
  public static partial Regex TvgLeakRegex();

  [GeneratedRegex(@"\[.*?\]")]
  public static partial Regex BracketCleanRegex();

  [GeneratedRegex(@"\s+\b(19\d{2}|20\d{2})\b\s*$")]
  public static partial Regex TrailingYearRegex();
}

// ============================================
// TMDB CLIENT
// ============================================

public class TmdbEnricher(
    TMDbClient tmdbClient,
    IFusionCache cache)
{
  private const int MaxPlotLength = 600;
  private static readonly Dictionary<int, string> MovieGenres = new()
    {
        {28,"Action"},{12,"Adventure"},{16,"Animation"},{35,"Comedy"},{80,"Crime"},
        {99,"Documentary"},{18,"Drama"},{10751,"Family"},{14,"Fantasy"},{36,"History"},
        {27,"Horror"},{10402,"Music"},{9648,"Mystery"},{10749,"Romance"},{878,"Science Fiction"},
        {10770,"TV Movie"},{53,"Thriller"},{10752,"War"},{37,"Western"}
    };

  private static readonly Dictionary<int, string> TvGenres = new()
    {
        {10759,"Action & Adventure"},{16,"Animation"},{35,"Comedy"},{80,"Crime"},{99,"Documentary"},
        {18,"Drama"},{10751,"Family"},{10762,"Kids"},{9648,"Mystery"},{10763,"News"},
        {10764,"Reality"},{10765,"Sci-Fi & Fantasy"},{10766,"Soap"},{10767,"Talk"},
        {10768,"War & Politics"},{37,"Western"}
    };

  public async Task<Movie?> SearchMovieAsync(string title, int? yearHint)
  {
    try
    {
      if (tmdbClient.ApiKey is not { Length: > 0 })
        return null;

      if (await cache.TryGetAsync<Movie>($"search-movie-{title}-{yearHint}") is { HasValue: true } cached)
        return cached.Value;

      if (await tmdbClient.SearchMovieAsync(StringHelpers.CleanQueryTitle(title), year: yearHint ?? 0) is not { Results: [{ } result] })
        return null;

      var detail = await tmdbClient.GetMovieAsync(result.Id,
      MovieMethods.Credits
       | MovieMethods.Videos
       | MovieMethods.Images
       | MovieMethods.ReleaseDates
       | MovieMethods.ExternalIds
       );

      await cache.SetAsync($"search-movie-{title}-{yearHint}", detail, TimeSpan.FromDays(7));
      return detail;
    }
    catch (TMDbLib.Objects.Exceptions.RequestLimitExceededException rl)
    {
      return null;
    }
  }

  public async Task<TvShow?> SearchSeriesAsync(string title)
  {
    try
    {
      if (tmdbClient.ApiKey is not { Length: > 0 })
        return null;

      if (await cache.TryGetAsync<TvShow>($"search-series-{title}") is { HasValue: true } cached)
        return cached.Value;

      if (await tmdbClient.SearchTvShowAsync(StringHelpers.CleanQueryTitle(title)) is not { Results: [{ } result] })
        return null;

      var detail = await tmdbClient.GetTvShowAsync(result.Id,
      TvShowMethods.Credits
       | TvShowMethods.Videos
       | TvShowMethods.Images
       | TvShowMethods.EpisodeGroups
       | TvShowMethods.ExternalIds
       );

      await cache.SetAsync($"search-series-{title}", detail, TimeSpan.FromDays(7));
      return detail;
    }
    catch (TMDbLib.Objects.Exceptions.RequestLimitExceededException rl)
    {
      return null;
    }
  }

}

// ============================================
// M3U PARSER
// ============================================

public class M3uParser
{
  private readonly ILogger<M3uParser> _logger;

  public M3uParser(ILogger<M3uParser> logger) => _logger = logger;

  public async IAsyncEnumerable<MovieItem> ParseMovies(Stream text)
  {
    using var reader = new StreamReader(text);
    var meta = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    string title = "";
    while (true)
    {
      var line = await reader.ReadLineAsync();
      if (line is null) break;
      if (line.StartsWith("#EXTINF:"))
      {
        var match = RegexPatterns.ExtinfRegex().Match(line);
        if (!match.Success)
        {
          title = "";
          meta.Clear();
          continue;
        }


        var attrsRaw = match.Groups["attrs"].Value;
        var rawTitle = match.Groups["title"].Value;

        meta = ParseM3uAttributes(attrsRaw);
        title = StringHelpers.CleanTitle(rawTitle);
      }
      else if (!string.IsNullOrEmpty(line) && !line.StartsWith("#") && !string.IsNullOrEmpty(title))
      {
        var url = line.Trim();
        var pathId = url.Split('/').Last();
        var streamId = Math.Abs(("movie:" + pathId).HashId());
        var poster = meta.ContainsKey("tvg-logo") ? meta["tvg-logo"] : null;
        var year = StringHelpers.GuessYear(title, meta);
        var ext = InferExtension(url);

        yield return new MovieItem(
            streamId,
            streamId,
            title,
            poster,
            year,
            "", // category_id filled later
            ext,
            url
        );

        meta.Clear();
        title = "";
      }
    }
  }

  public async IAsyncEnumerable<(SeriesInfo series, EpisodeItem episode)> ParseSeries(Stream text)
  {
    string title = "";
    var meta = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    using var reader = new StreamReader(text);
    while (true)
    {
      var line = await reader.ReadLineAsync();
      if (line is null) break;
      if (line.StartsWith("#EXTINF:"))
      {
        var match = RegexPatterns.ExtinfRegex().Match(line);
        if (!match.Success)
        {
          title = "";
          meta.Clear();
          continue;
        }

        var attrsRaw = match.Groups["attrs"].Value;
        var rawTitle = match.Groups["title"].Value;
        meta = ParseM3uAttributes(attrsRaw);
        title = StringHelpers.CleanTitle(rawTitle);
      }
      else if (!string.IsNullOrEmpty(line) && !line.StartsWith("#") && !string.IsNullOrEmpty(title))
      {
        var url = line.Trim();
        var pathId = url.Split('/').Last();
        var poster = meta.ContainsKey("tvg-logo") ? meta["tvg-logo"] : null;
        var raw = title;

        int season = 1, episode = 1;
        string showName = raw, epTitle = raw;

        if (RegexPatterns.DailyRegex().Match(raw) is { Success: true } dMatch)
        {
          var year = dMatch.Groups[1].Value;
          var month = dMatch.Groups[2].Value;
          var day = dMatch.Groups[3].Value;
          season = int.Parse(year) * 10000 + int.Parse(month) * 100 + int.Parse(day);
          episode = 1;
          showName = raw.Substring(0, raw.IndexOf(dMatch.Value) - 1).Trim(" -:_".ToCharArray());
          epTitle = raw;
        }
        else if (RegexPatterns.SeasonEpisodeRegex().Match(raw) is { Success: true } seMatch)
        {
          season = int.Parse(seMatch.Groups[1].Value);
          episode = int.Parse(seMatch.Groups[2].Value);
          showName = RegexPatterns.SeasonEpisodeRegex().Replace(raw, "").Trim(" -:_".ToCharArray());
          epTitle = raw;
        }
        else if (RegexPatterns.SeasonOnlyRegex().Match(raw) is { Success: true } sMatch)
        {
          season = int.Parse(sMatch.Groups[1].Value);
          episode = 1;
          showName = RegexPatterns.SeasonOnlyRegex().Replace(raw, "").Trim(" -:_".ToCharArray());
          epTitle = raw;
        }


        var seriesId = Math.Abs(("series:" + showName.ToLowerInvariant()).HashId());
        var episodeId = Math.Abs(("ep:" + pathId).HashId());
        var ext = InferExtension(url);

        var show = new SeriesInfo(seriesId, showName, poster);

        var ep = new EpisodeItem(
            episodeId,
            epTitle,
            season,
            episode,
            seriesId,
            poster,
            ext,
            url
        );

        yield return (show, ep);

        meta.Clear();
        title = "";
      }
    }
  }

  private Dictionary<string, string> ParseM3uAttributes(string attrsRaw)
  {
    var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    foreach (var match in RegexPatterns.M3uAttributeRegex().Matches(attrsRaw).Cast<Match>())
    {
      var key = match.Groups[1].Value.ToLowerInvariant();
      var value = match.Groups[2].Value;
      result[key] = value;

      if (key == "tvg_logo" && !result.ContainsKey("tvg-logo"))
        result["tvg-logo"] = value;
    }
    return result;
  }

  private static string InferExtension(string url)
  {
    var lower = url.ToLowerInvariant();
    foreach (var ext in new[] { "m3u8", "mp4", "mkv", "ts" })
    {
      if (lower.EndsWith("." + ext) || lower.Contains("." + ext + "?"))
        return ext;
    }
    return "mp4";
  }
}

// ============================================
// QUALITY & SOURCE HELPERS
// ============================================

public static class QualityAndSourceHelpers
{
  private static readonly (string name, Regex rx)[] SourcePatterns =
  {
    ("remux", new Regex(@"(?i)\bremux\b")),
    ("bluray", new Regex(@"(?i)\bblu[- ]?ray|bdrip|brrip|bdremux\b")),
    ("webrip", new Regex(@"(?i)\bweb[- ]?rip\b")),
    ("web", new Regex(@"(?i)\bweb[- ]?dl\b|\bweb\b")),
    ("dvd", new Regex(@"(?i)\bdvd|dvdrip\b")),
    ("hdtv", new Regex(@"(?i)\bhdtv\b")),
    ("ts", new Regex(@"(?i)\bts\b")),
    ("tc", new Regex(@"(?i)\btc\b")),
    ("cam", new Regex(@"(?i)\bcam\b")),
  };

  public static string ExtractQuality(string title)
  {
    var t = (title ?? "").Trim().ToLowerInvariant();
    if (t.Contains("2160p") || Regex.IsMatch(t, @"\b4k\b")) return "2160p";
    if (t.Contains("1080p")) return "1080p";
    if (t.Contains("720p")) return "720p";
    if (t.Contains("480p") || Regex.IsMatch(t, @"\bsd\b")) return "480p";
    return "other";
  }

  public static string ExtractSource(string title)
  {
    var t = (title ?? "").Trim().ToLowerInvariant();
    foreach (var (name, rx) in SourcePatterns)
    {
      if (rx.IsMatch(t)) return name;
    }
    return "other";
  }

  public static int PreferenceIndex(string value, IReadOnlyList<string> ordered)
  {
    var normalized = (value ?? "").Trim().ToLowerInvariant();
    for (int i = 0; i < ordered.Count; i++)
    {
      if (ordered[i] == normalized) return i;
    }
    return ordered.Count + 5;
  }
}

// ============================================
// STRING HELPERS
// ============================================

public static partial class StringHelpers
{
  [GeneratedRegex(@"\[.*?\]")]
  public static partial Regex CleanTitleRegex();

  public static int HashId(this string s)
  {
    var hash = MD5.HashData(Encoding.UTF8.GetBytes(s));
    return BitConverter.ToInt32(hash.TakeLast(4).ToArray());
  }
  public static string CleanTitle(string s)
  {
    var leak = s.IndexOf("\" tvg-");
    if (leak != -1)
      s = s[..leak];

    var sb = new StringBuilder();
    foreach (var ch in s)
    {
      if (ch == '\t' || ch == '\n' || (ch >= 0x20 && ch <= 0x10FFFF))
        sb.Append(ch);
    }
    s = sb.ToString();
    s = CleanTitleRegex().Replace(s, "").TrimEnd(); // remove bracketed
    return s.Length > 300 ? s[..300] : s;
  }

  public static string CleanQueryTitle(string raw)
  {
    var s = RegexPatterns.BracketedRegex().Replace(raw, " ");
    s = RegexPatterns.DustRegex().Replace(s, " ");
    s = s.Replace('.', ' ').Replace('_', ' ');
    s = RegexPatterns.TrailingYearRegex().Replace(s, " ");
    s = RegexPatterns.MultiSpaceRegex().Replace(s, " ").Trim();
    return s;
  }

  public static int? GuessYear(string title, Dictionary<string, string> meta)
  {
    var y = meta.TryGetValue("tvg-year", out var tyear) ? tyear :
            meta.TryGetValue("tvg_year", out var tyear2) ? tyear2 : "";
    if (!string.IsNullOrEmpty(y) && RegexPatterns.YearRegex().IsMatch(y))
      return int.Parse(y);

    var m = RegexPatterns.YearRegex().Match(title ?? "");
    return m.Success ? int.Parse(m.Groups[1].Value) : (int?)null;
  }

  public static string NormalizeTitleForGrouping(string title)
  {
    return CleanQueryTitle(title).ToLowerInvariant();
  }
}
