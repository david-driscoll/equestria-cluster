#!/usr/bin/dotnet run
#:package System.Collections.Immutable@10.0.0-preview.5.25277.114
#:package Spectre.Console@0.50.0
#:package Spectre.Console.Json@0.50.0
#:package Dumpify@0.6.6
#:package System.Net.Http.Json@10.0.1
#:package Refit@9.0.2
#:package NodaTime@*

using System.Text.Json;
using System.Text.Json.Serialization;
using Dumpify;
using NodaTime;
using Refit;
using Spectre;
using Spectre.Console;

var vikunjaToken = Environment.GetEnvironmentVariable("VIKUNJA_TOKEN");
if (string.IsNullOrWhiteSpace(vikunjaToken))
{
  throw new InvalidOperationException("The VIKUNJA_TOKEN environment variable must be set.");
}

var client = RestService.For<IVikunjaAPI>("https://vikunja.driscoll.tech/api/v1", new RefitSettings()
{
  AuthorizationHeaderValueGetter = async (request, token) => vikunjaToken!,
  ContentSerializer = new SystemTextJsonContentSerializer(SourceGenerationContext.Default.Options),
});

var tasks = await client.TasksGet(per_page: 500, filter: "done = false && due_date < now");

foreach (var task in tasks)
{
  task.Title.Dump("Title");
  try
  {
    var t = await client.TasksGet(task.Id);
    var dueDate = GetDueDate(task).ToDateTimeOffset();
    dueDate.Dump("New Due Date");
    // TODO: Spread this date out over based on labels and the project
    t.DueDate = dueDate;
    await client.TasksPost(task.Id, t);
  }
  catch (Exception ex)
  {
    ex.Dump();
  }
}

ZonedDateTime GetDueDate(Task task)
{
  var dueDate = ZonedDateTime.FromDateTimeOffset(task.DueDate!.Value);
  var zone = Environment.GetEnvironmentVariable("TZ") is { Length: > 0 } tz ? DateTimeZoneProviders.Tzdb[tz] : DateTimeZoneProviders.Tzdb["America/New_York"];
  var todayDate = LocalDateTime.FromDateTime(DateTime.Now).With(_ => new LocalTime(0, 0)).InZoneLeniently(zone);
  var isMorning = task.Labels.Any(z => z.Title.Equals("morning", StringComparison.OrdinalIgnoreCase));
  var isAfterWork = task.Labels.Any(z => z.Title.Equals("after work", StringComparison.OrdinalIgnoreCase)) && todayDate.DayOfWeek is not IsoDayOfWeek.Sunday and not IsoDayOfWeek.Saturday;

  return (dueDate.TimeOfDay, isMorning, isAfterWork) switch
  {
    ({ Hour: >= 22 }, var m, var w) => todayDate.Plus(Duration.FromHours(w ? 17 : 7) + Duration.FromDays(1)),
    ({ Hour: < 7 }, _, _) => todayDate.Plus(Duration.FromHours(7)),
    ({ Hour: < 17 } time, _, true) => todayDate.Plus(Duration.FromHours(time.Hour > 7 ? 17 : 7)),
    _ => todayDate.Plus(Duration.FromMinutes(30)),
  };
}
#region Vikunja
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Task))]
[JsonSerializable(typeof(FileResponse))]
[JsonSerializable(typeof(ProjectViewViewKind))]
[JsonSerializable(typeof(ProjectViewBucketConfigurationMode))]
[JsonSerializable(typeof(HTTPError))]
[JsonSerializable(typeof(VikunjaInfos))]
[JsonSerializable(typeof(OpenIDAuthInfo))]
[JsonSerializable(typeof(LocalAuthInfo))]
[JsonSerializable(typeof(LegalInfo))]
[JsonSerializable(typeof(LdapAuthInfo))]
[JsonSerializable(typeof(AuthInfo))]
[JsonSerializable(typeof(UserWithSettings))]
[JsonSerializable(typeof(UserSettings))]
[JsonSerializable(typeof(UserRegister))]
[JsonSerializable(typeof(UserPasswordConfirmation))]
[JsonSerializable(typeof(UserPassword))]
[JsonSerializable(typeof(UserExportStatus))]
[JsonSerializable(typeof(UserDeletionRequestConfirm))]
[JsonSerializable(typeof(UserAvatarProvider))]
[JsonSerializable(typeof(LinkShareAuth))]
[JsonSerializable(typeof(User))]
[JsonSerializable(typeof(Token2))]
[JsonSerializable(typeof(TOTPPasscode))]
[JsonSerializable(typeof(TOTP))]
[JsonSerializable(typeof(PasswordTokenRequest))]
[JsonSerializable(typeof(PasswordReset))]
[JsonSerializable(typeof(Login))]
[JsonSerializable(typeof(EmailUpdate))]
[JsonSerializable(typeof(EmailConfirm))]
[JsonSerializable(typeof(Migration3))]
[JsonSerializable(typeof(Migration2))]
[JsonSerializable(typeof(Callback))]
[JsonSerializable(typeof(DatabaseNotification))]
[JsonSerializable(typeof(Webhook))]
[JsonSerializable(typeof(UserWithPermission))]
[JsonSerializable(typeof(TeamWithPermission))]
[JsonSerializable(typeof(TeamUser))]
[JsonSerializable(typeof(TeamProject))]
[JsonSerializable(typeof(TeamMember))]
[JsonSerializable(typeof(Team))]
[JsonSerializable(typeof(TaskUnreadStatus))]
[JsonSerializable(typeof(TaskRepeatMode))]
[JsonSerializable(typeof(TaskReminder))]
[JsonSerializable(typeof(TaskRelation))]
[JsonSerializable(typeof(TaskPosition))]
[JsonSerializable(typeof(TaskComment))]
[JsonSerializable(typeof(TaskCollection))]
[JsonSerializable(typeof(TaskBucket))]
[JsonSerializable(typeof(TaskAttachment))]
[JsonSerializable(typeof(SharingType))]
[JsonSerializable(typeof(SavedFilter))]
[JsonSerializable(typeof(RouteDetail))]
[JsonSerializable(typeof(ReminderRelation))]
[JsonSerializable(typeof(RelationKind))]
[JsonSerializable(typeof(RelatedTaskMap))]
[JsonSerializable(typeof(ReactionMap))]
[JsonSerializable(typeof(Reaction))]
[JsonSerializable(typeof(ProjectViewBucketConfiguration))]
[JsonSerializable(typeof(ProjectView))]
[JsonSerializable(typeof(ProjectUser))]
[JsonSerializable(typeof(ProjectDuplicate))]
[JsonSerializable(typeof(Project))]
[JsonSerializable(typeof(Permission))]
[JsonSerializable(typeof(Message))]
[JsonSerializable(typeof(LinkSharing))]
[JsonSerializable(typeof(LabelTaskBulk))]
[JsonSerializable(typeof(LabelTask))]
[JsonSerializable(typeof(Label))]
[JsonSerializable(typeof(DatabaseNotifications))]
[JsonSerializable(typeof(BulkTask))]
[JsonSerializable(typeof(BulkAssignees))]
[JsonSerializable(typeof(Bucket))]
[JsonSerializable(typeof(APITokenRoute))]
[JsonSerializable(typeof(APIToken))]
[JsonSerializable(typeof(Status))]
[JsonSerializable(typeof(Migration))]
[JsonSerializable(typeof(AuthURL))]
[JsonSerializable(typeof(File))]
[JsonSerializable(typeof(Provider))]
[JsonSerializable(typeof(Image))]
[JsonSerializable(typeof(Token))]
internal partial class SourceGenerationContext : JsonSerializerContext { }

#pragma warning disable 108 // Disable "CS0108 '{derivedDto}.ToJson()' hides inherited member '{dtoBase}.ToJson()'. Use the new keyword if hiding was intended."
#pragma warning disable 114 // Disable "CS0114 '{derivedDto}.RaisePropertyChanged(String)' hides inherited member 'dtoBase.RaisePropertyChanged(String)'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword."
#pragma warning disable 472 // Disable "CS0472 The result of the expression is always 'false' since a value of type 'Int32' is never equal to 'null' of type 'Int32?'
#pragma warning disable 612 // Disable "CS0612 '...' is obsolete"
#pragma warning disable 649 // Disable "CS0649 Field is never assigned to, and will always have its default value null"
#pragma warning disable 1573 // Disable "CS1573 Parameter '...' has no matching param tag in the XML comment for ...
#pragma warning disable 1591 // Disable "CS1591 Missing XML comment for publicly visible type or member ..."
#pragma warning disable 8073 // Disable "CS8073 The result of the expression is always 'false' since a value of type 'T' is never equal to 'null' of type 'T?'"
#pragma warning disable 3016 // Disable "CS3016 Arrays as attribute arguments is not CLS-compliant"
#pragma warning disable 8600 // Disable "CS8600 Converting null literal or possible null value to non-nullable type"
#pragma warning disable 8602 // Disable "CS8602 Dereference of a possibly null reference"
#pragma warning disable 8603 // Disable "CS8603 Possible null reference return"
#pragma warning disable 8604 // Disable "CS8604 Possible null reference argument for parameter"
#pragma warning disable 8625 // Disable "CS8625 Cannot convert null literal to non-nullable reference type"
#pragma warning disable 8765 // Disable "CS8765 Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes)."
#pragma warning disable 8618 // Disable "CS8765 Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes)."

/// <summary>Vikunja API</summary>
[Headers("Authorization: Bearer")]
[System.CodeDom.Compiler.GeneratedCode("Refitter", "1.7.1.0")]
public partial interface IVikunjaAPI
{
  /// <summary>Authenticate a user with OpenID Connect</summary>
  /// <remarks>After a redirect from the OpenID Connect provider to the frontend has been made with the authentication `code`, this endpoint can be used to obtain a jwt token for that user and thus log them in.</remarks>
  /// <param name="callback">The openid callback</param>
  /// <param name="provider">The OpenID Connect provider key as returned by the /info endpoint</param>
  /// <returns>OK</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Post("/auth/openid/{provider}/callback")]
  Task<Token> AuthOpenidCallback(int provider, [Body] Callback callback);

  /// <summary>Get an unsplash image</summary>
  /// <remarks>Get an unsplash image. **Returns json on error.**</remarks>
  /// <param name="image">Unsplash Image ID</param>
  /// <returns>The image</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>404</term>
  /// <description>The image does not exist.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Get("/backgrounds/unsplash/image/{image}")]
  Task<FileResponse> BackgroundsUnsplashImage(int image);

  /// <summary>Get an unsplash thumbnail image</summary>
  /// <remarks>Get an unsplash thumbnail image. The thumbnail is cropped to a max width of 200px. **Returns json on error.**</remarks>
  /// <param name="image">Unsplash Image ID</param>
  /// <returns>The thumbnail</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>404</term>
  /// <description>The image does not exist.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Get("/backgrounds/unsplash/image/{image}/thumb")]
  Task<FileResponse> BackgroundsUnsplashImageThumb(int image);

  /// <summary>Search for a background from unsplash</summary>
  /// <remarks>Search for a project background from unsplash</remarks>
  /// <param name="s">Search backgrounds from unsplash with this search term.</param>
  /// <param name="p">The page number. Used for pagination. If not provided, the first page of results is returned.</param>
  /// <returns>An array with photos</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Get("/backgrounds/unsplash/search")]
  Task<ICollection<Image>> BackgroundsUnsplashSearch([Query] string? s = default, [Query] int? p = default);

  /// <summary>Creates a new saved filter</summary>
  /// <remarks>Creates a new saved filter</remarks>
  /// <returns>The Saved Filter</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>403</term>
  /// <description>The user does not have access to that saved filter.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Put("/filters")]
  Task<SavedFilter> FiltersPut();

  /// <summary>Gets one saved filter</summary>
  /// <remarks>Returns a saved filter by its ID.</remarks>
  /// <param name="id">Filter ID</param>
  /// <returns>The Saved Filter</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>403</term>
  /// <description>The user does not have access to that saved filter.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Get("/filters/{id}")]
  Task<SavedFilter> FiltersGet(int id);

  /// <summary>Updates a saved filter</summary>
  /// <remarks>Updates a saved filter by its ID.</remarks>
  /// <param name="id">Filter ID</param>
  /// <returns>The Saved Filter</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>403</term>
  /// <description>The user does not have access to that saved filter.</description>
  /// </item>
  /// <item>
  /// <term>404</term>
  /// <description>The saved filter does not exist.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Post("/filters/{id}")]
  Task<SavedFilter> FiltersPost(int id);

  /// <summary>Removes a saved filter</summary>
  /// <remarks>Removes a saved filter by its ID.</remarks>
  /// <param name="id">Filter ID</param>
  /// <returns>The Saved Filter</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>403</term>
  /// <description>The user does not have access to that saved filter.</description>
  /// </item>
  /// <item>
  /// <term>404</term>
  /// <description>The saved filter does not exist.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Delete("/filters/{id}")]
  Task<SavedFilter> FiltersDelete(int id);

  /// <summary>Info</summary>
  /// <remarks>Returns the version, frontendurl, motd and various settings of Vikunja</remarks>
  /// <returns>OK</returns>
  /// <exception cref="ApiException">Thrown when the request returns a non-success status code.</exception>
  [Get("/info")]
  Task<VikunjaInfos> Info();

  /// <summary>Get all labels a user has access to</summary>
  /// <remarks>Returns all labels which are either created by the user or associated with a task the user has at least read-access to.</remarks>
  /// <param name="page">The page number. Used for pagination. If not provided, the first page of results is returned.</param>
  /// <param name="per_page">The maximum number of items per page. Note this parameter is limited by the configured maximum of items per page.</param>
  /// <param name="s">Search labels by label text.</param>
  /// <returns>The labels</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Get("/labels")]
  Task<ICollection<Label>> LabelsGet([Query] int? page = default, [Query] int? per_page = default, [Query] string? s = default);

  /// <summary>Create a label</summary>
  /// <remarks>Creates a new label.</remarks>
  /// <param name="label">The label object</param>
  /// <returns>The created label object.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Invalid label object provided.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Put("/labels")]
  Task<Label> LabelsPut([Body] Label label);

  /// <summary>Gets one label</summary>
  /// <remarks>Returns one label by its ID.</remarks>
  /// <param name="id">Label ID</param>
  /// <returns>The label</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>403</term>
  /// <description>The user does not have access to the label</description>
  /// </item>
  /// <item>
  /// <term>404</term>
  /// <description>Label not found</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Get("/labels/{id}")]
  Task<Label> LabelsGet(int id);

  /// <summary>Update a label</summary>
  /// <remarks>Update an existing label. The user needs to be the creator of the label to be able to do this.</remarks>
  /// <param name="id">Label ID</param>
  /// <param name="label">The label object</param>
  /// <returns>The created label object.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Invalid label object provided.</description>
  /// </item>
  /// <item>
  /// <term>403</term>
  /// <description>Not allowed to update the label.</description>
  /// </item>
  /// <item>
  /// <term>404</term>
  /// <description>Label not found.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Put("/labels/{id}")]
  Task<Label> LabelsPut(int id, [Body] Label label);

  /// <summary>Delete a label</summary>
  /// <remarks>Delete an existing label. The user needs to be the creator of the label to be able to do this.</remarks>
  /// <param name="id">Label ID</param>
  /// <returns>The label was successfully deleted.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>403</term>
  /// <description>Not allowed to delete the label.</description>
  /// </item>
  /// <item>
  /// <term>404</term>
  /// <description>Label not found.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Delete("/labels/{id}")]
  Task<Label> LabelsDelete(int id);

  /// <summary>Login</summary>
  /// <remarks>Logs a user in. Returns a JWT-Token to authenticate further requests.</remarks>
  /// <param name="credentials">The login credentials</param>
  /// <returns>OK</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Invalid user password model.</description>
  /// </item>
  /// <item>
  /// <term>403</term>
  /// <description>Invalid username or password.</description>
  /// </item>
  /// <item>
  /// <term>412</term>
  /// <description>Invalid totp passcode.</description>
  /// </item>
  /// </list>
  /// </exception>
  [Post("/login")]
  Task<Token> Login([Body] Login credentials);

  /// <summary>Get the auth url from Microsoft Todo</summary>
  /// <remarks>Returns the auth url where the user needs to get its auth code. This code can then be used to migrate everything from Microsoft Todo to Vikunja.</remarks>
  /// <returns>The auth url.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>500</term>
  /// <description>Internal server error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Get("/migration/microsoft-todo/auth")]
  Task<AuthURL> MigrationMicrosoftTodoAuth();

  /// <summary>Migrate all projects, tasks etc. from Microsoft Todo</summary>
  /// <remarks>Migrates all tasklinsts, tasks, notes and reminders from Microsoft Todo to Vikunja.</remarks>
  /// <param name="migrationCode">The auth token previously obtained from the auth url. See the docs for /migration/microsoft-todo/auth.</param>
  /// <returns>A message telling you everything was migrated successfully.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>500</term>
  /// <description>Internal server error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Post("/migration/microsoft-todo/migrate")]
  Task<Message> MigrationMicrosoftTodoMigrate([Body] Migration migrationCode);

  /// <summary>Get migration status</summary>
  /// <remarks>Returns if the current user already did the migation or not. This is useful to show a confirmation message in the frontend if the user is trying to do the same migration again.</remarks>
  /// <returns>The migration status</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>500</term>
  /// <description>Internal server error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Get("/migration/microsoft-todo/status")]
  Task<Status> MigrationMicrosoftTodoStatus();

  /// <summary>Import all projects, tasks etc. from a TickTick backup export</summary>
  /// <remarks>Imports all projects, tasks, notes, reminders, subtasks and files from a TickTick backup export into Vikunja.</remarks>
  /// <param name="import">The TickTick backup csv file.</param>
  /// <returns>A message telling you everything was migrated successfully.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>500</term>
  /// <description>Internal server error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Post("/migration/ticktick/migrate")]
  Task<Message> MigrationTicktickMigrate(string import);

  /// <summary>Get migration status</summary>
  /// <remarks>Returns if the current user already did the migation or not. This is useful to show a confirmation message in the frontend if the user is trying to do the same migration again.</remarks>
  /// <returns>The migration status</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>500</term>
  /// <description>Internal server error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Get("/migration/ticktick/status")]
  Task<Status> MigrationTicktickStatus();

  /// <summary>Get the auth url from todoist</summary>
  /// <remarks>Returns the auth url where the user needs to get its auth code. This code can then be used to migrate everything from todoist to Vikunja.</remarks>
  /// <returns>The auth url.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>500</term>
  /// <description>Internal server error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Get("/migration/todoist/auth")]
  Task<AuthURL> MigrationTodoistAuth();

  /// <summary>Migrate all lists, tasks etc. from todoist</summary>
  /// <remarks>Migrates all projects, tasks, notes, reminders, subtasks and files from todoist to vikunja.</remarks>
  /// <param name="migrationCode">The auth code previously obtained from the auth url. See the docs for /migration/todoist/auth.</param>
  /// <returns>A message telling you everything was migrated successfully.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>500</term>
  /// <description>Internal server error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Post("/migration/todoist/migrate")]
  Task<Message> MigrationTodoistMigrate([Body] Migration2 migrationCode);

  /// <summary>Get migration status</summary>
  /// <remarks>Returns if the current user already did the migation or not. This is useful to show a confirmation message in the frontend if the user is trying to do the same migration again.</remarks>
  /// <returns>The migration status</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>500</term>
  /// <description>Internal server error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Get("/migration/todoist/status")]
  Task<Status> MigrationTodoistStatus();

  /// <summary>Get the auth url from trello</summary>
  /// <remarks>Returns the auth url where the user needs to get its auth code. This code can then be used to migrate everything from trello to Vikunja.</remarks>
  /// <returns>The auth url.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>500</term>
  /// <description>Internal server error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Get("/migration/trello/auth")]
  Task<AuthURL> MigrationTrelloAuth();

  /// <summary>Migrate all projects, tasks etc. from trello</summary>
  /// <remarks>Migrates all projects, tasks, notes, reminders, subtasks and files from trello to vikunja.</remarks>
  /// <param name="migrationCode">The auth token previously obtained from the auth url. See the docs for /migration/trello/auth.</param>
  /// <returns>A message telling you everything was migrated successfully.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>500</term>
  /// <description>Internal server error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Post("/migration/trello/migrate")]
  Task<Message> MigrationTrelloMigrate([Body] Migration3 migrationCode);

  /// <summary>Get migration status</summary>
  /// <remarks>Returns if the current user already did the migation or not. This is useful to show a confirmation message in the frontend if the user is trying to do the same migration again.</remarks>
  /// <returns>The migration status</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>500</term>
  /// <description>Internal server error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Get("/migration/trello/status")]
  Task<Status> MigrationTrelloStatus();

  /// <summary>Import all projects, tasks etc. from a Vikunja data export</summary>
  /// <remarks>Imports all projects, tasks, notes, reminders, subtasks and files from a Vikunjda data export into Vikunja.</remarks>
  /// <param name="import">The Vikunja export zip file.</param>
  /// <returns>A message telling you everything was migrated successfully.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>500</term>
  /// <description>Internal server error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Post("/migration/vikunja-file/migrate")]
  Task<Message> MigrationVikunjaFileMigrate(string import);

  /// <summary>Get migration status</summary>
  /// <remarks>Returns if the current user already did the migation or not. This is useful to show a confirmation message in the frontend if the user is trying to do the same migration again.</remarks>
  /// <returns>The migration status</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>500</term>
  /// <description>Internal server error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Get("/migration/vikunja-file/status")]
  Task<Status> MigrationVikunjaFileStatus();

  /// <summary>Get all notifications for the current user</summary>
  /// <remarks>Returns an array with all notifications for the current user.</remarks>
  /// <param name="page">The page number. Used for pagination. If not provided, the first page of results is returned.</param>
  /// <param name="per_page">The maximum number of items per page. Note this parameter is limited by the configured maximum of items per page.</param>
  /// <returns>The notifications</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>403</term>
  /// <description>Link shares cannot have notifications.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Get("/notifications")]
  Task<ICollection<DatabaseNotification>> NotificationsGet([Query] int? page = default, [Query] int? per_page = default);

  /// <summary>Mark all notifications of a user as read</summary>
  /// <returns>All notifications marked as read.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Post("/notifications")]
  Task<Message> NotificationsPost();

  /// <summary>Mark a notification as (un-)read</summary>
  /// <remarks>Marks a notification as either read or unread. A user can only mark their own notifications as read.</remarks>
  /// <param name="id">Notification ID</param>
  /// <returns>The notification to mark as read.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>403</term>
  /// <description>Link shares cannot have notifications.</description>
  /// </item>
  /// <item>
  /// <term>404</term>
  /// <description>The notification does not exist.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Post("/notifications/{id}")]
  Task<DatabaseNotifications> NotificationsPost(int id);

  /// <summary>Get all projects a user has access to</summary>
  /// <remarks>Returns all projects a user has access to.</remarks>
  /// <param name="page">The page number. Used for pagination. If not provided, the first page of results is returned.</param>
  /// <param name="per_page">The maximum number of items per page. Note this parameter is limited by the configured maximum of items per page.</param>
  /// <param name="s">Search projects by title.</param>
  /// <param name="is_archived">If true, also returns all archived projects.</param>
  /// <param name="expand">If set to `permissions`, Vikunja will return the max permission the current user has on this project. You can currently only set this to `permissions`.</param>
  /// <returns>The projects</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>403</term>
  /// <description>The user does not have access to the project</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Get("/projects")]
  Task<ICollection<Project>> ProjectsGet([Query] int? page = default, [Query] int? per_page = default, [Query] string? s = default, [Query] bool? is_archived = default, [Query] string? expand = default);

  /// <summary>Creates a new project</summary>
  /// <remarks>Creates a new project. If a parent project is provided the user needs to have write access to that project.</remarks>
  /// <param name="project">The project you want to create.</param>
  /// <returns>The created project.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Invalid project object provided.</description>
  /// </item>
  /// <item>
  /// <term>403</term>
  /// <description>The user does not have access to the project</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Put("/projects")]
  Task<Project> ProjectsPut([Body] Project project);

  /// <summary>Gets one project</summary>
  /// <remarks>Returns a project by its ID.</remarks>
  /// <param name="id">Project ID</param>
  /// <returns>The project</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>403</term>
  /// <description>The user does not have access to the project</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Get("/projects/{id}")]
  Task<Project> ProjectsGet(int id);

  /// <summary>Updates a project</summary>
  /// <remarks>Updates a project. This does not include adding a task (see below).</remarks>
  /// <param name="id">Project ID</param>
  /// <param name="project">The project with updated values you want to update.</param>
  /// <returns>The updated project.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Invalid project object provided.</description>
  /// </item>
  /// <item>
  /// <term>403</term>
  /// <description>The user does not have access to the project</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Post("/projects/{id}")]
  Task<Project> ProjectsPost(int id, [Body] Project project);

  /// <summary>Deletes a project</summary>
  /// <remarks>Delets a project</remarks>
  /// <param name="id">Project ID</param>
  /// <returns>The project was successfully deleted.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Invalid project object provided.</description>
  /// </item>
  /// <item>
  /// <term>403</term>
  /// <description>The user does not have access to the project</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Delete("/projects/{id}")]
  Task<Message> ProjectsDelete(int id);

  /// <summary>Get the project background</summary>
  /// <remarks>Get the project background of a specific project. **Returns json on error.**</remarks>
  /// <param name="id">Project ID</param>
  /// <returns>The project background file.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>403</term>
  /// <description>No access to this project.</description>
  /// </item>
  /// <item>
  /// <term>404</term>
  /// <description>The project does not exist.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Get("/projects/{id}/background")]
  Task<FileResponse> ProjectsBackgroundGet(int id);

  /// <summary>Remove a project background</summary>
  /// <remarks>Removes a previously set project background, regardless of the project provider used to set the background. It does not throw an error if the project does not have a background.</remarks>
  /// <param name="id">Project ID</param>
  /// <returns>The project</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>403</term>
  /// <description>No access to this project.</description>
  /// </item>
  /// <item>
  /// <term>404</term>
  /// <description>The project does not exist.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Delete("/projects/{id}/background")]
  Task<Project> ProjectsBackgroundDelete(int id);

  /// <summary>Set an unsplash photo as project background</summary>
  /// <remarks>Sets a photo from unsplash as project background.</remarks>
  /// <param name="id">Project ID</param>
  /// <param name="project">The image you want to set as background</param>
  /// <returns>The background has been successfully set.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Invalid image object provided.</description>
  /// </item>
  /// <item>
  /// <term>403</term>
  /// <description>The user does not have access to the project</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Post("/projects/{id}/backgrounds/unsplash")]
  Task<Project> ProjectsBackgroundsUnsplash(int id, [Body] Image project);

  /// <summary>Upload a project background</summary>
  /// <remarks>Upload a project background.</remarks>
  /// <param name="id">Project ID</param>
  /// <param name="background">The file as single file.</param>
  /// <returns>The background was set successfully.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>File is no image.</description>
  /// </item>
  /// <item>
  /// <term>403</term>
  /// <description>File too large.</description>
  /// </item>
  /// <item>
  /// <term>404</term>
  /// <description>The project does not exist.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Multipart]
  [Put("/projects/{id}/backgrounds/upload")]
  Task<Message> ProjectsBackgroundsUpload(int id, string background);

  /// <summary>Get users</summary>
  /// <remarks>Lists all users (without emailadresses). Also possible to search for a specific user.</remarks>
  /// <param name="s">Search for a user by its name.</param>
  /// <param name="id">Project ID</param>
  /// <returns>All (found) users.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Something\'s invalid.</description>
  /// </item>
  /// <item>
  /// <term>401</term>
  /// <description>The user does not have the permission to see the project.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal server error.</description>
  /// </item>
  /// </list>
  /// </exception>
  [Get("/projects/{id}/projectusers")]
  Task<ICollection<User>> ProjectsProjectusers(int id, [Query] string? s = default);

  /// <summary>Create a task</summary>
  /// <remarks>Inserts a task into a project.</remarks>
  /// <param name="id">Project ID</param>
  /// <param name="task">The task object</param>
  /// <returns>The created task object.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Invalid task object provided.</description>
  /// </item>
  /// <item>
  /// <term>403</term>
  /// <description>The user does not have access to the project</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Put("/projects/{id}/tasks")]
  Task<Task> ProjectsTasks(int id, [Body] Task task);

  /// <summary>Get teams on a project</summary>
  /// <remarks>Returns a project with all teams which have access on a given project.</remarks>
  /// <param name="id">Project ID</param>
  /// <param name="page">The page number. Used for pagination. If not provided, the first page of results is returned.</param>
  /// <param name="per_page">The maximum number of items per page. Note this parameter is limited by the configured maximum of items per page.</param>
  /// <param name="s">Search teams by its name.</param>
  /// <returns>The teams with their permission.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>403</term>
  /// <description>No permission to see the project.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Get("/projects/{id}/teams")]
  Task<ICollection<TeamWithPermission>> ProjectsTeamsGet(int id, [Query] int? page = default, [Query] int? per_page = default, [Query] string? s = default);

  /// <summary>Add a team to a project</summary>
  /// <remarks>Gives a team access to a project.</remarks>
  /// <param name="id">Project ID</param>
  /// <param name="project">The team you want to add to the project.</param>
  /// <returns>The created team<->project relation.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Invalid team project object provided.</description>
  /// </item>
  /// <item>
  /// <term>403</term>
  /// <description>The user does not have access to the project</description>
  /// </item>
  /// <item>
  /// <term>404</term>
  /// <description>The team does not exist.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Put("/projects/{id}/teams")]
  Task<TeamProject> ProjectsTeamsPut(int id, [Body] TeamProject project);

  /// <summary>Get users on a project</summary>
  /// <remarks>Returns a project with all users which have access on a given project.</remarks>
  /// <param name="id">Project ID</param>
  /// <param name="page">The page number. Used for pagination. If not provided, the first page of results is returned.</param>
  /// <param name="per_page">The maximum number of items per page. Note this parameter is limited by the configured maximum of items per page.</param>
  /// <param name="s">Search users by its name.</param>
  /// <returns>The users with the permission they have.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>403</term>
  /// <description>No permission to see the project.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Get("/projects/{id}/users")]
  Task<ICollection<UserWithPermission>> ProjectsUsersGet(int id, [Query] int? page = default, [Query] int? per_page = default, [Query] string? s = default);

  /// <summary>Add a user to a project</summary>
  /// <remarks>Gives a user access to a project.</remarks>
  /// <param name="id">Project ID</param>
  /// <param name="project">The user you want to add to the project.</param>
  /// <returns>The created user<->project relation.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Invalid user project object provided.</description>
  /// </item>
  /// <item>
  /// <term>403</term>
  /// <description>The user does not have access to the project</description>
  /// </item>
  /// <item>
  /// <term>404</term>
  /// <description>The user does not exist.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Put("/projects/{id}/users")]
  Task<ProjectUser> ProjectsUsersPut(int id, [Body] ProjectUser project);

  /// <summary>Get all kanban buckets of a project</summary>
  /// <remarks>Returns all kanban buckets which belong to that project. Buckets are always sorted by their `position` in ascending order. To get all buckets with their tasks, use the tasks endpoint with a kanban view.</remarks>
  /// <param name="id">Project ID</param>
  /// <param name="view">Project view ID</param>
  /// <returns>The buckets</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>500</term>
  /// <description>Internal server error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Get("/projects/{id}/views/{view}/buckets")]
  Task<ICollection<Bucket>> ProjectsViewsBucketsGet(int id, int view);

  /// <summary>Create a new bucket</summary>
  /// <remarks>Creates a new kanban bucket on a project.</remarks>
  /// <param name="id">Project Id</param>
  /// <param name="view">Project view ID</param>
  /// <param name="bucket">The bucket object</param>
  /// <returns>The created bucket object.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Invalid bucket object provided.</description>
  /// </item>
  /// <item>
  /// <term>404</term>
  /// <description>The project does not exist.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Put("/projects/{id}/views/{view}/buckets")]
  Task<Bucket> ProjectsViewsBucketsPut(int id, int view, [Body] Bucket bucket);

  /// <summary>Get tasks in a project</summary>
  /// <remarks>Returns all tasks for the selected project. When the requested view is a kanban view, a list of buckets containing the tasks will be returned. Otherwise, a list of tasks will be returned.</remarks>
  /// <param name="id">The project ID.</param>
  /// <param name="view">The project view ID.</param>
  /// <param name="page">The page number. Used for pagination. If not provided, the first page of results is returned.</param>
  /// <param name="per_page">The maximum number of items per page. Note this parameter is limited by the configured maximum of items per page.</param>
  /// <param name="s">Search tasks by task text.</param>
  /// <param name="sort_by">The sorting parameter. You can pass this multiple times to get the tasks ordered by multiple different parametes, along with `order_by`. Possible values to sort by are `id`, `title`, `description`, `done`, `done_at`, `due_date`, `created_by_id`, `project_id`, `repeat_after`, `priority`, `start_date`, `end_date`, `hex_color`, `percent_done`, `uid`, `created`, `updated`. Default is `id`.</param>
  /// <param name="order_by">The ordering parameter. Possible values to order by are `asc` or `desc`. Default is `asc`.</param>
  /// <param name="filter">The filter query to match tasks by. Check out https://vikunja.io/docs/filters for a full explanation of the feature.</param>
  /// <param name="filter_timezone">The time zone which should be used for date match (statements like</param>
  /// <param name="filter_include_nulls">If set to true the result will include filtered fields whose value is set to `null`. Available values are `true` or `false`. Defaults to `false`.</param>
  /// <param name="expand">If set to `subtasks`, Vikunja will fetch only tasks which do not have subtasks and then in a second step, will fetch all of these subtasks. This may result in more tasks than the pagination limit being returned, but all subtasks will be present in the response. If set to `buckets`, the buckets of each task will be present in the response. If set to `reactions`, the reactions of each task will be present in the response. If set to `comments`, the first 50 comments of each task will be present in the response. You can set this multiple times with different values.</param>
  /// <returns>The tasks</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Get("/projects/{id}/views/{view}/tasks")]
  Task<ICollection<Task>> ProjectsViewsTasks(int id, int view, [Query] int? page = default, [Query] int? per_page = default, [Query] string? s = default, [Query] string? sort_by = default, [Query] string? order_by = default, [Query] string? filter = default, [Query] string? filter_timezone = default, [Query] string? filter_include_nulls = default, [Query(CollectionFormat.Multi)] IEnumerable<object>? expand = default);

  /// <summary>Get all api webhook targets for the specified project</summary>
  /// <remarks>Get all api webhook targets for the specified project.</remarks>
  /// <param name="page">The page number. Used for pagination. If not provided, the first page of results is returned.</param>
  /// <param name="per_page">The maximum number of items per bucket per page. This parameter is limited by the configured maximum of items per page.</param>
  /// <param name="id">Project ID</param>
  /// <returns>The list of all webhook targets</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>500</term>
  /// <description>Internal server error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Get("/projects/{id}/webhooks")]
  Task<ICollection<Webhook>> ProjectsWebhooksGet(int id, [Query] int? page = default, [Query] int? per_page = default);

  /// <summary>Create a webhook target</summary>
  /// <remarks>Create a webhook target which receives POST requests about specified events from a project.</remarks>
  /// <param name="id">Project ID</param>
  /// <param name="webhook">The webhook target object with required fields</param>
  /// <returns>The created webhook target.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Invalid webhook object provided.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Put("/projects/{id}/webhooks")]
  Task<Webhook> ProjectsWebhooksPut(int id, [Body] Webhook webhook);

  /// <summary>Change a webhook target's events.</summary>
  /// <remarks>Change a webhook target's events. You cannot change other values of a webhook.</remarks>
  /// <param name="id">Project ID</param>
  /// <param name="webhookID">Webhook ID</param>
  /// <returns>Updated webhook target</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>404</term>
  /// <description>The webhok target does not exist</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Post("/projects/{id}/webhooks/{webhookID}")]
  Task<Webhook> ProjectsWebhooksPost(int id, int webhookID);

  /// <summary>Deletes an existing webhook target</summary>
  /// <remarks>Delete any of the project's webhook targets.</remarks>
  /// <param name="id">Project ID</param>
  /// <param name="webhookID">Webhook ID</param>
  /// <returns>Successfully deleted.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>404</term>
  /// <description>The webhok target does not exist.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Delete("/projects/{id}/webhooks/{webhookID}")]
  Task<Message> ProjectsWebhooksDelete(int id, int webhookID);

  /// <summary>Duplicate an existing project</summary>
  /// <remarks>Copies the project, tasks, files, kanban data, assignees, comments, attachments, labels, relations, backgrounds, user/team permissions and link shares from one project to a new one. The user needs read access in the project and write access in the parent of the new project.</remarks>
  /// <param name="projectID">The project ID to duplicate</param>
  /// <param name="project">The target parent project which should hold the copied project.</param>
  /// <returns>The created project.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Invalid project duplicate object provided.</description>
  /// </item>
  /// <item>
  /// <term>403</term>
  /// <description>The user does not have access to the project or its parent.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Put("/projects/{projectID}/duplicate")]
  Task<ProjectDuplicate> ProjectsDuplicate(int projectID, [Body] ProjectDuplicate project);

  /// <summary>Update a team &lt;-&gt; project relation</summary>
  /// <remarks>Update a team &lt;-&gt; project relation. Mostly used to update the permission that team has.</remarks>
  /// <param name="projectID">Project ID</param>
  /// <param name="teamID">Team ID</param>
  /// <param name="project">The team you want to update.</param>
  /// <returns>The updated team <-> project relation.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>403</term>
  /// <description>The user does not have admin-access to the project</description>
  /// </item>
  /// <item>
  /// <term>404</term>
  /// <description>Team or project does not exist.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Post("/projects/{projectID}/teams/{teamID}")]
  Task<TeamProject> ProjectsTeamsPost(int projectID, int teamID, [Body] TeamProject project);

  /// <summary>Delete a team from a project</summary>
  /// <remarks>Delets a team from a project. The team won't have access to the project anymore.</remarks>
  /// <param name="projectID">Project ID</param>
  /// <param name="teamID">Team ID</param>
  /// <returns>The team was successfully deleted.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>403</term>
  /// <description>The user does not have access to the project</description>
  /// </item>
  /// <item>
  /// <term>404</term>
  /// <description>Team or project does not exist.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Delete("/projects/{projectID}/teams/{teamID}")]
  Task<Message> ProjectsTeamsDelete(int projectID, int teamID);

  /// <summary>Update a user &lt;-&gt; project relation</summary>
  /// <remarks>Update a user &lt;-&gt; project relation. Mostly used to update the permission that user has.</remarks>
  /// <param name="projectID">Project ID</param>
  /// <param name="userID">User ID</param>
  /// <param name="project">The user you want to update.</param>
  /// <returns>The updated user <-> project relation.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>403</term>
  /// <description>The user does not have admin-access to the project</description>
  /// </item>
  /// <item>
  /// <term>404</term>
  /// <description>User or project does not exist.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Post("/projects/{projectID}/users/{userID}")]
  Task<ProjectUser> ProjectsUsersPost(int projectID, int userID, [Body] ProjectUser project);

  /// <summary>Delete a user from a project</summary>
  /// <remarks>Delets a user from a project. The user won't have access to the project anymore.</remarks>
  /// <param name="projectID">Project ID</param>
  /// <param name="userID">User ID</param>
  /// <returns>The user was successfully removed from the project.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>403</term>
  /// <description>The user does not have access to the project</description>
  /// </item>
  /// <item>
  /// <term>404</term>
  /// <description>user or project does not exist.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Delete("/projects/{projectID}/users/{userID}")]
  Task<Message> ProjectsUsersDelete(int projectID, int userID);

  /// <summary>Update an existing bucket</summary>
  /// <remarks>Updates an existing kanban bucket.</remarks>
  /// <param name="projectID">Project Id</param>
  /// <param name="bucketID">Bucket Id</param>
  /// <param name="view">Project view ID</param>
  /// <param name="bucket">The bucket object</param>
  /// <returns>The created bucket object.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Invalid bucket object provided.</description>
  /// </item>
  /// <item>
  /// <term>404</term>
  /// <description>The bucket does not exist.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Post("/projects/{projectID}/views/{view}/buckets/{bucketID}")]
  Task<Bucket> ProjectsViewsBucketsPost(int projectID, int bucketID, int view, [Body] Bucket bucket);

  /// <summary>Deletes an existing bucket</summary>
  /// <remarks>Deletes an existing kanban bucket and dissociates all of its task. It does not delete any tasks. You cannot delete the last bucket on a project.</remarks>
  /// <param name="projectID">Project Id</param>
  /// <param name="bucketID">Bucket Id</param>
  /// <param name="view">Project view ID</param>
  /// <returns>Successfully deleted.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>404</term>
  /// <description>The bucket does not exist.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Delete("/projects/{projectID}/views/{view}/buckets/{bucketID}")]
  Task<Message> ProjectsViewsBucketsDelete(int projectID, int bucketID, int view);

  /// <summary>Get all link shares for a project</summary>
  /// <remarks>Returns all link shares which exist for a given project</remarks>
  /// <param name="project">Project ID</param>
  /// <param name="page">The page number. Used for pagination. If not provided, the first page of results is returned.</param>
  /// <param name="per_page">The maximum number of items per page. Note this parameter is limited by the configured maximum of items per page.</param>
  /// <param name="s">Search shares by hash.</param>
  /// <returns>The share links</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Get("/projects/{project}/shares")]
  Task<ICollection<LinkSharing>> ProjectsSharesGet(int project, [Query] int? page = default, [Query] int? per_page = default, [Query] string? s = default);

  /// <summary>Share a project via link</summary>
  /// <remarks>Share a project via link. The user needs to have write-access to the project to be able do this.</remarks>
  /// <param name="project">Project ID</param>
  /// <param name="label">The new link share object</param>
  /// <returns>The created link share object.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Invalid link share object provided.</description>
  /// </item>
  /// <item>
  /// <term>403</term>
  /// <description>Not allowed to add the project share.</description>
  /// </item>
  /// <item>
  /// <term>404</term>
  /// <description>The project does not exist.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Put("/projects/{project}/shares")]
  Task<LinkSharing> ProjectsSharesPut(int project, [Body] LinkSharing label);

  /// <summary>Get one link shares for a project</summary>
  /// <remarks>Returns one link share by its ID.</remarks>
  /// <param name="project">Project ID</param>
  /// <param name="share">Share ID</param>
  /// <returns>The share links</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>403</term>
  /// <description>No access to the project</description>
  /// </item>
  /// <item>
  /// <term>404</term>
  /// <description>Share Link not found.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Get("/projects/{project}/shares/{share}")]
  Task<LinkSharing> ProjectsSharesGet(int project, int share);

  /// <summary>Remove a link share</summary>
  /// <remarks>Remove a link share. The user needs to have write-access to the project to be able do this.</remarks>
  /// <param name="project">Project ID</param>
  /// <param name="share">Share Link ID</param>
  /// <returns>The link was successfully removed.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>403</term>
  /// <description>Not allowed to remove the link.</description>
  /// </item>
  /// <item>
  /// <term>404</term>
  /// <description>Share Link not found.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Delete("/projects/{project}/shares/{share}")]
  Task<Message> ProjectsSharesDelete(int project, int share);

  /// <summary>Get all project views for a project</summary>
  /// <remarks>Returns all project views for a sepcific project</remarks>
  /// <param name="project">Project ID</param>
  /// <returns>The project views</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Get("/projects/{project}/views")]
  Task<ICollection<ProjectView>> ProjectsViewsGet(int project);

  /// <summary>Create a project view</summary>
  /// <remarks>Create a project view in a specific project.</remarks>
  /// <param name="project">Project ID</param>
  /// <param name="view">The project view you want to create.</param>
  /// <returns>The created project view</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>403</term>
  /// <description>The user does not have access to create a project view</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Put("/projects/{project}/views")]
  Task<ProjectView> ProjectsViewsPut(int project, [Body] ProjectView view);

  /// <summary>Get one project view</summary>
  /// <remarks>Returns a project view by its ID.</remarks>
  /// <param name="project">Project ID</param>
  /// <param name="id">Project View ID</param>
  /// <returns>The project view</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>403</term>
  /// <description>The user does not have access to this project view</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Get("/projects/{project}/views/{id}")]
  Task<ProjectView> ProjectsViewsGet(int project, int id);

  /// <summary>Updates a project view</summary>
  /// <remarks>Updates a project view.</remarks>
  /// <param name="project">Project ID</param>
  /// <param name="id">Project View ID</param>
  /// <param name="view">The project view with updated values you want to change.</param>
  /// <returns>The updated project view.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Invalid project view object provided.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Post("/projects/{project}/views/{id}")]
  Task<ProjectView> ProjectsViewsPost(int project, int id, [Body] ProjectView view);

  /// <summary>Delete a project view</summary>
  /// <remarks>Deletes a project view.</remarks>
  /// <param name="project">Project ID</param>
  /// <param name="id">Project View ID</param>
  /// <returns>The project view was successfully deleted.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>403</term>
  /// <description>The user does not have access to the project view</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Delete("/projects/{project}/views/{id}")]
  Task<Message> ProjectsViewsDelete(int project, int id);

  /// <summary>Update a task bucket</summary>
  /// <remarks>Updates a task in a bucket</remarks>
  /// <param name="project">Project ID</param>
  /// <param name="view">Project View ID</param>
  /// <param name="bucket">Bucket ID</param>
  /// <param name="taskBucket">The id of the task you want to move into the bucket.</param>
  /// <returns>The updated task bucket.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Invalid task bucket object provided.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Post("/projects/{project}/views/{view}/buckets/{bucket}/tasks")]
  Task<TaskBucket> ProjectsViewsBucketsTasks(int project, int view, int bucket, [Body] TaskBucket taskBucket);

  /// <summary>Register</summary>
  /// <remarks>Creates a new user account.</remarks>
  /// <param name="credentials">The user with credentials to create</param>
  /// <returns>OK</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>No or invalid user register object provided / User already exists.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Post("/register")]
  Task<User> Register([Body] UserRegister credentials);

  /// <summary>Get a list of all token api routes</summary>
  /// <remarks>Returns a list of all API routes which are available to use with an api token, not a user login.</remarks>
  /// <returns>The list of all routes.</returns>
  /// <exception cref="ApiException">Thrown when the request returns a non-success status code.</exception>
  [Get("/routes")]
  Task<ICollection<APITokenRoute>> Routes();

  /// <summary>Get an auth token for a share</summary>
  /// <remarks>Get a jwt auth token for a shared project from a share hash.</remarks>
  /// <param name="password">The password for link shares which require one.</param>
  /// <param name="share">The share hash</param>
  /// <returns>The valid jwt auth token.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Invalid link share object provided.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Post("/shares/{share}/auth")]
  Task<Token> SharesAuth(string share, [Body] LinkShareAuth password);

  /// <summary>Subscribes the current user to an entity.</summary>
  /// <remarks>Subscribes the current user to an entity.</remarks>
  /// <param name="entity">The entity the user subscribes to. Can be either `project` or `task`.</param>
  /// <param name="entityID">The numeric id of the entity to subscribe to.</param>
  /// <returns>The subscription</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>403</term>
  /// <description>The user does not have access to subscribe to this entity.</description>
  /// </item>
  /// <item>
  /// <term>412</term>
  /// <description>The subscription entity is invalid.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Put("/subscriptions/{entity}/{entityID}")]
  Task<Subscription> SubscriptionsPut(string entity, string entityID);

  /// <summary>Unsubscribe the current user from an entity.</summary>
  /// <remarks>Unsubscribes the current user to an entity.</remarks>
  /// <param name="entity">The entity the user subscribed to. Can be either `project` or `task`.</param>
  /// <param name="entityID">The numeric id of the subscribed entity to.</param>
  /// <returns>The subscription</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>403</term>
  /// <description>The user does not have access to subscribe to this entity.</description>
  /// </item>
  /// <item>
  /// <term>404</term>
  /// <description>The subscription does not exist.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Delete("/subscriptions/{entity}/{entityID}")]
  Task<Subscription> SubscriptionsDelete(string entity, string entityID);

  /// <summary>Get tasks</summary>
  /// <remarks>Returns all tasks on any project the user has access to.</remarks>
  /// <param name="page">The page number. Used for pagination. If not provided, the first page of results is returned.</param>
  /// <param name="per_page">The maximum number of items per page. Note this parameter is limited by the configured maximum of items per page.</param>
  /// <param name="s">Search tasks by task text.</param>
  /// <param name="sort_by">The sorting parameter. You can pass this multiple times to get the tasks ordered by multiple different parametes, along with `order_by`. Possible values to sort by are `id`, `title`, `description`, `done`, `done_at`, `due_date`, `created_by_id`, `project_id`, `repeat_after`, `priority`, `start_date`, `end_date`, `hex_color`, `percent_done`, `uid`, `created`, `updated`. Default is `id`.</param>
  /// <param name="order_by">The ordering parameter. Possible values to order by are `asc` or `desc`. Default is `asc`.</param>
  /// <param name="filter">The filter query to match tasks by. Check out https://vikunja.io/docs/filters for a full explanation of the feature.</param>
  /// <param name="filter_timezone">The time zone which should be used for date match (statements like</param>
  /// <param name="filter_include_nulls">If set to true the result will include filtered fields whose value is set to `null`. Available values are `true` or `false`. Defaults to `false`.</param>
  /// <param name="expand">If set to `subtasks`, Vikunja will fetch only tasks which do not have subtasks and then in a second step, will fetch all of these subtasks. This may result in more tasks than the pagination limit being returned, but all subtasks will be present in the response. If set to `buckets`, the buckets of each task will be present in the response. If set to `reactions`, the reactions of each task will be present in the response. If set to `comments`, the first 50 comments of each task will be present in the response. You can set this multiple times with different values.</param>
  /// <returns>The tasks</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Get("/tasks")]
  Task<ICollection<Task>> TasksGet([Query] int? page = default, [Query] int? per_page = default, [Query] string? s = default, [Query] string? sort_by = default, [Query] string? order_by = default, [Query] string? filter = default, [Query] string? filter_timezone = default, [Query] string? filter_include_nulls = default, [Query(CollectionFormat.Multi)] IEnumerable<string>? expand = default);

  /// <summary>Update multiple tasks</summary>
  /// <remarks>Updates multiple tasks atomically. All provided tasks must be writable by the user.</remarks>
  /// <param name="bulkTask">Bulk task update payload</param>
  /// <returns>Updated tasks</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Invalid request</description>
  /// </item>
  /// <item>
  /// <term>403</term>
  /// <description>The user does not have access to the tasks</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Post("/tasks/bulk")]
  Task<ICollection<Task>> TasksBulk([Body] BulkTask bulkTask);

  /// <summary>Get one task</summary>
  /// <remarks>Returns one task by its ID</remarks>
  /// <param name="id">The task ID</param>
  /// <param name="expand">If set to `subtasks`, Vikunja will fetch only tasks which do not have subtasks and then in a second step, will fetch all of these subtasks. This may result in more tasks than the pagination limit being returned, but all subtasks will be present in the response. If set to `buckets`, the buckets of each task will be present in the response. If set to `reactions`, the reactions of each task will be present in the response. If set to `comments`, the first 50 comments of each task will be present in the response. You can set this multiple times with different values.</param>
  /// <returns>The task</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>404</term>
  /// <description>Task not found</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Get("/tasks/{id}")]
  Task<Task> TasksGet(int id, [Query(CollectionFormat.Multi)] IEnumerable<string>? expand = default);

  /// <summary>Update a task</summary>
  /// <remarks>Updates a task. This includes marking it as done. Assignees you pass will be updated, see their individual endpoints for more details on how this is done. To update labels, see the description of the endpoint.</remarks>
  /// <param name="id">The Task ID</param>
  /// <param name="task">The task object</param>
  /// <returns>The updated task object.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Invalid task object provided.</description>
  /// </item>
  /// <item>
  /// <term>403</term>
  /// <description>The user does not have access to the task (aka its project)</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Post("/tasks/{id}")]
  Task<Task> TasksPost(int id, [Body] Task task);

  /// <summary>Delete a task</summary>
  /// <remarks>Deletes a task from a project. This does not mean "mark it done".</remarks>
  /// <param name="id">Task ID</param>
  /// <returns>The created task object.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Invalid task ID provided.</description>
  /// </item>
  /// <item>
  /// <term>403</term>
  /// <description>The user does not have access to the project</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Delete("/tasks/{id}")]
  Task<Message> TasksDelete(int id);

  /// <summary>Get  all attachments for one task.</summary>
  /// <remarks>Get all task attachments for one task.</remarks>
  /// <param name="id">Task ID</param>
  /// <param name="page">The page number. Used for pagination. If not provided, the first page of results is returned.</param>
  /// <param name="per_page">The maximum number of items per page. Note this parameter is limited by the configured maximum of items per page.</param>
  /// <returns>All attachments for this task</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>403</term>
  /// <description>No access to this task.</description>
  /// </item>
  /// <item>
  /// <term>404</term>
  /// <description>The task does not exist.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Get("/tasks/{id}/attachments")]
  Task<ICollection<TaskAttachment>> TasksAttachmentsGet(int id, [Query] int? page = default, [Query] int? per_page = default);

  /// <summary>Upload a task attachment</summary>
  /// <remarks>Upload a task attachment. You can pass multiple files with the files form param.</remarks>
  /// <param name="id">Task ID</param>
  /// <param name="files">The file, as multipart form file. You can pass multiple.</param>
  /// <returns>Attachments were uploaded successfully.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>403</term>
  /// <description>No access to the task.</description>
  /// </item>
  /// <item>
  /// <term>404</term>
  /// <description>The task does not exist.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Multipart]
  [Put("/tasks/{id}/attachments")]
  Task<Message> TasksAttachmentsPut(int id, string files);

  /// <summary>Get one attachment.</summary>
  /// <remarks>Get one attachment for download. **Returns json on error.**</remarks>
  /// <param name="id">Task ID</param>
  /// <param name="attachmentID">Attachment ID</param>
  /// <param name="preview_size">The size of the preview image. Can be sm = 100px, md = 200px, lg = 400px or xl = 800px. If provided, a preview image will be returned if the attachment is an image.</param>
  /// <returns>The attachment file.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>403</term>
  /// <description>No access to this task.</description>
  /// </item>
  /// <item>
  /// <term>404</term>
  /// <description>The task does not exist.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Get("/tasks/{id}/attachments/{attachmentID}")]
  Task<FileResponse> TasksAttachmentsGet(int id, int attachmentID, [Query] string? preview_size = default);

  /// <summary>Delete an attachment</summary>
  /// <remarks>Delete an attachment.</remarks>
  /// <param name="id">Task ID</param>
  /// <param name="attachmentID">Attachment ID</param>
  /// <returns>The attachment was deleted successfully.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>403</term>
  /// <description>No access to this task.</description>
  /// </item>
  /// <item>
  /// <term>404</term>
  /// <description>The task does not exist.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Delete("/tasks/{id}/attachments/{attachmentID}")]
  Task<Message> TasksAttachmentsDelete(int id, int attachmentID);

  /// <summary>Updates a task position</summary>
  /// <remarks>Updates a task position.</remarks>
  /// <param name="id">Task ID</param>
  /// <param name="view">The task position with updated values you want to change.</param>
  /// <returns>The updated task position.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Invalid task position object provided.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Post("/tasks/{id}/position")]
  Task<TaskPosition> TasksPosition(int id, [Body] TaskPosition view);

  /// <summary>Mark a task as read</summary>
  /// <remarks>Marks a task as read for the current user by removing the unread status entry.</remarks>
  /// <param name="projecttask">Task ID</param>
  /// <returns>The task unread status object.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>403</term>
  /// <description>The user does not have access to the task</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Post("/tasks/{projecttask}/read")]
  Task<TaskUnreadStatus> TasksRead(int projecttask);

  /// <summary>Get all assignees for a task</summary>
  /// <remarks>Returns an array with all assignees for this task.</remarks>
  /// <param name="page">The page number. Used for pagination. If not provided, the first page of results is returned.</param>
  /// <param name="per_page">The maximum number of items per page. Note this parameter is limited by the configured maximum of items per page.</param>
  /// <param name="s">Search assignees by their username.</param>
  /// <param name="taskID">Task ID</param>
  /// <returns>The assignees</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Get("/tasks/{taskID}/assignees")]
  Task<ICollection<User>> TasksAssigneesGet(int taskID, [Query] int? page = default, [Query] int? per_page = default, [Query] string? s = default);

  /// <summary>Add a new assignee to a task</summary>
  /// <remarks>Adds a new assignee to a task. The assignee needs to have access to the project, the doer must be able to edit this task.</remarks>
  /// <param name="assignee">The assingee object</param>
  /// <param name="taskID">Task ID</param>
  /// <returns>The created assingee object.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Invalid assignee object provided.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Put("/tasks/{taskID}/assignees")]
  Task<TaskAssginee> TasksAssigneesPut(int taskID, [Body] TaskAssginee assignee);

  /// <summary>Add multiple new assignees to a task</summary>
  /// <remarks>Adds multiple new assignees to a task. The assignee needs to have access to the project, the doer must be able to edit this task. Every user not in the project will be unassigned from the task, pass an empty array to unassign everyone.</remarks>
  /// <param name="assignee">The array of assignees</param>
  /// <param name="taskID">Task ID</param>
  /// <returns>The created assingees object.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Invalid assignee object provided.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Post("/tasks/{taskID}/assignees/bulk")]
  Task<TaskAssginee> TasksAssigneesBulk(int taskID, [Body] BulkAssignees assignee);

  /// <summary>Delete an assignee</summary>
  /// <remarks>Un-assign a user from a task.</remarks>
  /// <param name="taskID">Task ID</param>
  /// <param name="userID">Assignee user ID</param>
  /// <returns>The assignee was successfully deleted.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>403</term>
  /// <description>Not allowed to delete the assignee.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Delete("/tasks/{taskID}/assignees/{userID}")]
  Task<Message> TasksAssigneesDelete(int taskID, int userID);

  /// <summary>Get all task comments</summary>
  /// <remarks>Get all task comments. The user doing this need to have at least read access to the task.</remarks>
  /// <param name="taskID">Task ID</param>
  /// <returns>The array with all task comments</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Get("/tasks/{taskID}/comments")]
  Task<ICollection<TaskComment>> TasksCommentsGet(int taskID);

  /// <summary>Create a new task comment</summary>
  /// <remarks>Create a new task comment. The user doing this need to have at least write access to the task this comment should belong to.</remarks>
  /// <param name="relation">The task comment object</param>
  /// <param name="taskID">Task ID</param>
  /// <returns>The created task comment object.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Invalid task comment object provided.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Put("/tasks/{taskID}/comments")]
  Task<TaskComment> TasksCommentsPut(int taskID, [Body] TaskComment relation);

  /// <summary>Remove a task comment</summary>
  /// <remarks>Remove a task comment. The user doing this need to have at least read access to the task this comment belongs to.</remarks>
  /// <param name="taskID">Task ID</param>
  /// <param name="commentID">Comment ID</param>
  /// <returns>The task comment object.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Invalid task comment object provided.</description>
  /// </item>
  /// <item>
  /// <term>404</term>
  /// <description>The task comment was not found.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Get("/tasks/{taskID}/comments/{commentID}")]
  Task<TaskComment> TasksCommentsGet(int taskID, int commentID);

  /// <summary>Update an existing task comment</summary>
  /// <remarks>Update an existing task comment. The user doing this need to have at least write access to the task this comment belongs to.</remarks>
  /// <param name="taskID">Task ID</param>
  /// <param name="commentID">Comment ID</param>
  /// <returns>The updated task comment object.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Invalid task comment object provided.</description>
  /// </item>
  /// <item>
  /// <term>404</term>
  /// <description>The task comment was not found.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Post("/tasks/{taskID}/comments/{commentID}")]
  Task<TaskComment> TasksCommentsPost(int taskID, int commentID);

  /// <summary>Remove a task comment</summary>
  /// <remarks>Remove a task comment. The user doing this need to have at least write access to the task this comment belongs to.</remarks>
  /// <param name="taskID">Task ID</param>
  /// <param name="commentID">Comment ID</param>
  /// <returns>The task comment was successfully deleted.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Invalid task comment object provided.</description>
  /// </item>
  /// <item>
  /// <term>404</term>
  /// <description>The task comment was not found.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Delete("/tasks/{taskID}/comments/{commentID}")]
  Task<Message> TasksCommentsDelete(int taskID, int commentID);

  /// <summary>Update all labels on a task.</summary>
  /// <remarks>Updates all labels on a task. Every label which is not passed but exists on the task will be deleted. Every label which does not exist on the task will be added. All labels which are passed and already exist on the task won't be touched.</remarks>
  /// <param name="label">The array of labels</param>
  /// <param name="taskID">Task ID</param>
  /// <returns>The updated labels object.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Invalid label object provided.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Post("/tasks/{taskID}/labels/bulk")]
  Task<LabelTaskBulk> TasksLabelsBulk(int taskID, [Body] LabelTaskBulk label);

  /// <summary>Create a new relation between two tasks</summary>
  /// <remarks>Creates a new relation between two tasks. The user needs to have update permissions on the base task and at least read permissions on the other task. Both tasks do not need to be on the same project. Take a look at the docs for available task relation kinds.</remarks>
  /// <param name="relation">The relation object</param>
  /// <param name="taskID">Task ID</param>
  /// <returns>The created task relation object.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Invalid task relation object provided.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Put("/tasks/{taskID}/relations")]
  Task<TaskRelation> TasksRelationsPut(int taskID, [Body] TaskRelation relation);

  /// <summary>Remove a task relation</summary>
  /// <param name="relation">The relation object</param>
  /// <param name="taskID">Task ID</param>
  /// <param name="relationKind">The kind of the relation. See the TaskRelation type for more info.</param>
  /// <param name="otherTaskID">The id of the other task.</param>
  /// <returns>The task relation was successfully deleted.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Invalid task relation object provided.</description>
  /// </item>
  /// <item>
  /// <term>404</term>
  /// <description>The task relation was not found.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Delete("/tasks/{taskID}/relations/{relationKind}/{otherTaskID}")]
  Task<Message> TasksRelationsDelete(int taskID, string relationKind, int otherTaskID, [Body] TaskRelation relation);

  /// <summary>Get all labels on a task</summary>
  /// <remarks>Returns all labels which are assicociated with a given task.</remarks>
  /// <param name="task">Task ID</param>
  /// <param name="page">The page number. Used for pagination. If not provided, the first page of results is returned.</param>
  /// <param name="per_page">The maximum number of items per page. Note this parameter is limited by the configured maximum of items per page.</param>
  /// <param name="s">Search labels by label text.</param>
  /// <returns>The labels</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Get("/tasks/{task}/labels")]
  Task<ICollection<Label>> TasksLabelsGet(int task, [Query] int? page = default, [Query] int? per_page = default, [Query] string? s = default);

  /// <summary>Add a label to a task</summary>
  /// <remarks>Add a label to a task. The user needs to have write-access to the project to be able do this.</remarks>
  /// <param name="task">Task ID</param>
  /// <param name="label">The label object</param>
  /// <returns>The created label relation object.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Invalid label object provided.</description>
  /// </item>
  /// <item>
  /// <term>403</term>
  /// <description>Not allowed to add the label.</description>
  /// </item>
  /// <item>
  /// <term>404</term>
  /// <description>The label does not exist.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Put("/tasks/{task}/labels")]
  Task<LabelTask> TasksLabelsPut(int task, [Body] LabelTask label);

  /// <summary>Remove a label from a task</summary>
  /// <remarks>Remove a label from a task. The user needs to have write-access to the project to be able do this.</remarks>
  /// <param name="task">Task ID</param>
  /// <param name="label">Label ID</param>
  /// <returns>The label was successfully removed.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>403</term>
  /// <description>Not allowed to remove the label.</description>
  /// </item>
  /// <item>
  /// <term>404</term>
  /// <description>Label not found.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Delete("/tasks/{task}/labels/{label}")]
  Task<Message> TasksLabelsDelete(int task, int label);

  /// <summary>Get teams</summary>
  /// <remarks>Returns all teams the current user is part of.</remarks>
  /// <param name="page">The page number. Used for pagination. If not provided, the first page of results is returned.</param>
  /// <param name="per_page">The maximum number of items per page. Note this parameter is limited by the configured maximum of items per page.</param>
  /// <param name="s">Search teams by its name.</param>
  /// <returns>The teams.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Get("/teams")]
  Task<ICollection<Team>> TeamsGet([Query] int? page = default, [Query] int? per_page = default, [Query] string? s = default);

  /// <summary>Creates a new team</summary>
  /// <remarks>Creates a new team.</remarks>
  /// <param name="team">The team you want to create.</param>
  /// <returns>The created team.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Invalid team object provided.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Put("/teams")]
  Task<Team> TeamsPut([Body] Team team);

  /// <summary>Gets one team</summary>
  /// <remarks>Returns a team by its ID.</remarks>
  /// <param name="id">Team ID</param>
  /// <returns>The team</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>403</term>
  /// <description>The user does not have access to the team</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Get("/teams/{id}")]
  Task<Team> TeamsGet(int id);

  /// <summary>Updates a team</summary>
  /// <remarks>Updates a team.</remarks>
  /// <param name="id">Team ID</param>
  /// <param name="team">The team with updated values you want to update.</param>
  /// <returns>The updated team.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Invalid team object provided.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Post("/teams/{id}")]
  Task<Team> TeamsPost(int id, [Body] Team team);

  /// <summary>Deletes a team</summary>
  /// <remarks>Delets a team. This will also remove the access for all users in that team.</remarks>
  /// <param name="id">Team ID</param>
  /// <returns>The team was successfully deleted.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Invalid team object provided.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Delete("/teams/{id}")]
  Task<Message> TeamsDelete(int id);

  /// <summary>Add a user to a team</summary>
  /// <remarks>Add a user to a team.</remarks>
  /// <param name="id">Team ID</param>
  /// <param name="team">The user to be added to a team.</param>
  /// <returns>The newly created member object</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Invalid member object provided.</description>
  /// </item>
  /// <item>
  /// <term>403</term>
  /// <description>The user does not have access to the team</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Put("/teams/{id}/members")]
  Task<TeamMember> TeamsMembersPut(int id, [Body] TeamMember team);

  /// <summary>Toggle a team member's admin status</summary>
  /// <remarks>If a user is team admin, this will make them member and vise-versa.</remarks>
  /// <param name="id">Team ID</param>
  /// <param name="userID">User ID</param>
  /// <returns>The member permission was successfully changed.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Post("/teams/{id}/members/{userID}/admin")]
  Task<Message> TeamsMembersAdmin(int id, int userID);

  /// <summary>Remove a user from a team</summary>
  /// <remarks>Remove a user from a team. This will also revoke any access this user might have via that team. A user can remove themselves from the team if they are not the last user in the team.</remarks>
  /// <param name="id">The ID of the team you want to remove th user from</param>
  /// <param name="username">The username of the user you want to remove</param>
  /// <returns>The user was successfully removed from the team.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Delete("/teams/{id}/members/{username}")]
  Task<Message> TeamsMembersDelete(int id, int username);

  /// <summary>Reset the db to a defined state</summary>
  /// <remarks>Fills the specified table with the content provided in the payload. You need to enable the testing endpoint before doing this and provide the `Authorization: &lt;token&gt;` secret when making requests to this endpoint. See docs for more details.</remarks>
  /// <param name="table">The table to reset</param>
  /// <returns>Everything has been imported successfully.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>500</term>
  /// <description>Internal server error.</description>
  /// </item>
  /// </list>
  /// </exception>
  [Patch("/test/{table}")]
  Task<ICollection<User>> Test(string table);

  /// <summary>Get all api tokens of the current user</summary>
  /// <remarks>Returns all api tokens the current user has created.</remarks>
  /// <param name="page">The page number, used for pagination. If not provided, the first page of results is returned.</param>
  /// <param name="per_page">The maximum number of tokens per page. This parameter is limited by the configured maximum of items per page.</param>
  /// <param name="s">Search tokens by their title.</param>
  /// <returns>The list of all tokens</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>500</term>
  /// <description>Internal server error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Get("/tokens")]
  Task<ICollection<APIToken>> TokensGet([Query] int? page = default, [Query] int? per_page = default, [Query] string? s = default);

  /// <summary>Create a new api token</summary>
  /// <remarks>Create a new api token to use on behalf of the user creating it.</remarks>
  /// <param name="token">The token object with required fields</param>
  /// <returns>The created token.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Invalid token object provided.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Put("/tokens")]
  Task<APIToken> TokensPut([Body] APIToken token);

  /// <summary>Deletes an existing api token</summary>
  /// <remarks>Delete any of the user's api tokens.</remarks>
  /// <param name="tokenID">Token ID</param>
  /// <returns>Successfully deleted.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>404</term>
  /// <description>The token does not exist.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Delete("/tokens/{tokenID}")]
  Task<Message> TokensDelete(int tokenID);

  /// <summary>Get user information</summary>
  /// <remarks>Returns the current user object with their settings.</remarks>
  /// <returns>OK</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>404</term>
  /// <description>User does not exist.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal server error.</description>
  /// </item>
  /// </list>
  /// </exception>
  [Get("/user")]
  Task<UserWithSettings> User();

  /// <summary>Confirm the email of a new user</summary>
  /// <remarks>Confirms the email of a newly registered user.</remarks>
  /// <param name="credentials">The token.</param>
  /// <returns>OK</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>412</term>
  /// <description>Bad token provided.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Post("/user/confirm")]
  Task<Message> UserConfirm([Body] EmailConfirm credentials);

  /// <summary>Abort a user deletion request</summary>
  /// <remarks>Aborts an in-progress user deletion.</remarks>
  /// <param name="credentials">The user password to confirm.</param>
  /// <returns>OK</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>412</term>
  /// <description>Bad password provided.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Post("/user/deletion/cancel")]
  Task<Message> UserDeletionCancel([Body] UserPasswordConfirmation credentials);

  /// <summary>Confirm a user deletion request</summary>
  /// <remarks>Confirms the deletion request of a user sent via email.</remarks>
  /// <param name="credentials">The token.</param>
  /// <returns>OK</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>412</term>
  /// <description>Bad token provided.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Post("/user/deletion/confirm")]
  Task<Message> UserDeletionConfirm([Body] UserDeletionRequestConfirm credentials);

  /// <summary>Request the deletion of the user</summary>
  /// <remarks>Requests the deletion of the current user. It will trigger an email which has to be confirmed to start the deletion.</remarks>
  /// <param name="credentials">The user password.</param>
  /// <returns>OK</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>412</term>
  /// <description>Bad password provided.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Post("/user/deletion/request")]
  Task<Message> UserDeletionRequest([Body] UserPasswordConfirmation credentials);

  /// <summary>Get current user data export</summary>
  /// <returns>OK</returns>
  /// <exception cref="ApiException">Thrown when the request returns a non-success status code.</exception>
  [Get("/user/export")]
  Task<UserExportStatus> UserExport();

  /// <summary>Download a user data export.</summary>
  /// <param name="password">User password to confirm the download.</param>
  /// <returns>OK</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Something\'s invalid.</description>
  /// </item>
  /// <item>
  /// <term>404</term>
  /// <description>No user data export found.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal server error.</description>
  /// </item>
  /// </list>
  /// </exception>
  [Post("/user/export/download")]
  Task<Message> UserExportDownload([Body] UserPasswordConfirmation password);

  /// <summary>Request a user data export.</summary>
  /// <param name="password">User password to confirm the data export request.</param>
  /// <returns>OK</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Something\'s invalid.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal server error.</description>
  /// </item>
  /// </list>
  /// </exception>
  [Post("/user/export/request")]
  Task<Message> UserExportRequest([Body] UserPasswordConfirmation password);

  /// <summary>Change password</summary>
  /// <remarks>Lets the current user change its password.</remarks>
  /// <param name="userPassword">The current and new password.</param>
  /// <returns>OK</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Something\'s invalid.</description>
  /// </item>
  /// <item>
  /// <term>404</term>
  /// <description>User does not exist.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal server error.</description>
  /// </item>
  /// </list>
  /// </exception>
  [Post("/user/password")]
  Task<Message> UserPassword([Body] UserPassword userPassword);

  /// <summary>Resets a password</summary>
  /// <remarks>Resets a user email with a previously reset token.</remarks>
  /// <param name="credentials">The token with the new password.</param>
  /// <returns>OK</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Bad token provided.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Post("/user/password/reset")]
  Task<Message> UserPasswordReset([Body] PasswordReset credentials);

  /// <summary>Request password reset token</summary>
  /// <remarks>Requests a token to reset a users password. The token is sent via email.</remarks>
  /// <param name="credentials">The username of the user to request a token for.</param>
  /// <returns>OK</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>404</term>
  /// <description>The user does not exist.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Post("/user/password/token")]
  Task<Message> UserPasswordToken([Body] PasswordTokenRequest credentials);

  /// <summary>Return user avatar setting</summary>
  /// <remarks>Returns the current user's avatar setting.</remarks>
  /// <returns>OK</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Something\'s invalid.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal server error.</description>
  /// </item>
  /// </list>
  /// </exception>
  [Get("/user/settings/avatar")]
  Task<UserAvatarProvider> UserSettingsAvatarGet();

  /// <summary>Set the user's avatar</summary>
  /// <remarks>Changes the user avatar. Valid types are gravatar (uses the user email), upload, initials, marble, ldap (synced from LDAP server), openid (synced from OpenID provider), default.</remarks>
  /// <param name="avatar">The user's avatar setting</param>
  /// <returns>OK</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Something\'s invalid.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal server error.</description>
  /// </item>
  /// </list>
  /// </exception>
  [Post("/user/settings/avatar")]
  Task<Message> UserSettingsAvatarPost([Body] UserAvatarProvider avatar);

  /// <summary>Upload a user avatar</summary>
  /// <remarks>Upload a user avatar. This will also set the user's avatar provider to "upload"</remarks>
  /// <param name="avatar">The avatar as single file.</param>
  /// <returns>The avatar was set successfully.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>File is no image.</description>
  /// </item>
  /// <item>
  /// <term>403</term>
  /// <description>File too large.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Multipart]
  [Put("/user/settings/avatar/upload")]
  Task<Message> UserSettingsAvatarUpload(string avatar);

  /// <summary>Update email address</summary>
  /// <remarks>Lets the current user change their email address.</remarks>
  /// <param name="userEmailUpdate">The new email address and current password.</param>
  /// <returns>OK</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Something\'s invalid.</description>
  /// </item>
  /// <item>
  /// <term>404</term>
  /// <description>User does not exist.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal server error.</description>
  /// </item>
  /// </list>
  /// </exception>
  [Post("/user/settings/email")]
  Task<Message> UserSettingsEmail([Body] EmailUpdate userEmailUpdate);

  /// <summary>Change general user settings of the current user.</summary>
  /// <param name="avatar">The updated user settings</param>
  /// <returns>OK</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Something\'s invalid.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal server error.</description>
  /// </item>
  /// </list>
  /// </exception>
  [Post("/user/settings/general")]
  Task<Message> UserSettingsGeneral([Body] UserSettings avatar);

  /// <summary>Returns the caldav tokens for the current user</summary>
  /// <remarks>Return the IDs and created dates of all caldav tokens for the current user.</remarks>
  /// <returns>OK</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Something\'s invalid.</description>
  /// </item>
  /// <item>
  /// <term>404</term>
  /// <description>User does not exist.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal server error.</description>
  /// </item>
  /// </list>
  /// </exception>
  [Get("/user/settings/token/caldav")]
  Task<ICollection<Token2>> UserSettingsTokenCaldavGet();

  /// <summary>Generate a caldav token</summary>
  /// <remarks>Generates a caldav token which can be used for the caldav api. It is not possible to see the token again after it was generated.</remarks>
  /// <returns>OK</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Something\'s invalid.</description>
  /// </item>
  /// <item>
  /// <term>404</term>
  /// <description>User does not exist.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal server error.</description>
  /// </item>
  /// </list>
  /// </exception>
  [Put("/user/settings/token/caldav")]
  Task<Token2> UserSettingsTokenCaldavPut();

  /// <summary>Delete a caldav token by id</summary>
  /// <param name="id">Token ID</param>
  /// <returns>OK</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Something\'s invalid.</description>
  /// </item>
  /// <item>
  /// <term>404</term>
  /// <description>User does not exist.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal server error.</description>
  /// </item>
  /// </list>
  /// </exception>
  [Delete("/user/settings/token/caldav/{id}")]
  Task<Message> UserSettingsTokenCaldavDelete(int id);

  /// <summary>Totp setting for the current user</summary>
  /// <remarks>Returns the current user totp setting or an error if it is not enabled.</remarks>
  /// <returns>The totp settings.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>500</term>
  /// <description>Internal server error.</description>
  /// </item>
  /// </list>
  /// </exception>
  [Get("/user/settings/totp")]
  Task<TOTP> UserSettingsTotp();

  /// <summary>Disable totp settings</summary>
  /// <remarks>Disables any totp settings for the current user.</remarks>
  /// <param name="totp">The current user's password (only password is enough).</param>
  /// <returns>Successfully disabled</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Something\'s invalid.</description>
  /// </item>
  /// <item>
  /// <term>404</term>
  /// <description>User does not exist.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal server error.</description>
  /// </item>
  /// </list>
  /// </exception>
  [Post("/user/settings/totp/disable")]
  Task<Message> UserSettingsTotpDisable([Body] Login totp);

  /// <summary>Enable a previously enrolled totp setting.</summary>
  /// <remarks>Enables a previously enrolled totp setting by providing a totp passcode.</remarks>
  /// <param name="totp">The totp passcode.</param>
  /// <returns>Successfully enabled</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Something\'s invalid.</description>
  /// </item>
  /// <item>
  /// <term>404</term>
  /// <description>User does not exist.</description>
  /// </item>
  /// <item>
  /// <term>412</term>
  /// <description>TOTP is not enrolled.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal server error.</description>
  /// </item>
  /// </list>
  /// </exception>
  [Post("/user/settings/totp/enable")]
  Task<Message> UserSettingsTotpEnable([Body] TOTPPasscode totp);

  /// <summary>Enroll a user into totp</summary>
  /// <remarks>Creates an initial setup for the user in the db. After this step, the user needs to verify they have a working totp setup with the "enable totp" endpoint.</remarks>
  /// <returns>OK</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Something\'s invalid.</description>
  /// </item>
  /// <item>
  /// <term>404</term>
  /// <description>User does not exist.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal server error.</description>
  /// </item>
  /// </list>
  /// </exception>
  [Post("/user/settings/totp/enroll")]
  Task<TOTP> UserSettingsTotpEnroll();

  /// <summary>Totp QR Code</summary>
  /// <remarks>Returns a qr code for easier setup at end user's devices.</remarks>
  /// <returns>The qr code as jpeg image</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>500</term>
  /// <description>Internal server error.</description>
  /// </item>
  /// </list>
  /// </exception>
  [Get("/user/settings/totp/qrcode")]
  Task<FileResponse> UserSettingsTotpQrcode();

  /// <summary>Get all available time zones on this vikunja instance</summary>
  /// <remarks>Because available time zones depend on the system Vikunja is running on, this endpoint returns a project of all valid time zones this particular Vikunja instance can handle. The project of time zones is not sorted, you should sort it on the client.</remarks>
  /// <returns>All available time zones.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>500</term>
  /// <description>Internal server error.</description>
  /// </item>
  /// </list>
  /// </exception>
  [Get("/user/timezones")]
  Task<ICollection<string>> UserTimezones();

  /// <summary>Renew user token</summary>
  /// <remarks>Returns a new valid jwt user token with an extended length.</remarks>
  /// <returns>OK</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Only user token are available for renew.</description>
  /// </item>
  /// </list>
  /// </exception>
  [Post("/user/token")]
  Task<Token> UserToken();

  /// <summary>Get users</summary>
  /// <remarks>Search for a user by its username, name or full email. Name (not username) or email require that the user has enabled this in their settings.</remarks>
  /// <param name="s">The search criteria.</param>
  /// <returns>All (found) users.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>400</term>
  /// <description>Something\'s invalid.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal server error.</description>
  /// </item>
  /// </list>
  /// </exception>
  [Get("/users")]
  Task<ICollection<User>> Users([Query] string? s = default);

  /// <summary>Get all possible webhook events</summary>
  /// <remarks>Get all possible webhook events to use when creating or updating a webhook target.</remarks>
  /// <returns>The list of all possible webhook events</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>500</term>
  /// <description>Internal server error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Get("/webhooks/events")]
  Task<ICollection<string>> WebhooksEvents();

  /// <summary>Get all reactions for an entity</summary>
  /// <remarks>Returns all reactions for an entity</remarks>
  /// <param name="id">Entity ID</param>
  /// <param name="kind">The kind of the entity. Can be either `tasks` or `comments` for task comments</param>
  /// <returns>The reactions</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>403</term>
  /// <description>The user does not have access to the entity</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Get("/{kind}/{id}/reactions")]
  Task<ICollection<ReactionMap>> ReactionsGet(int id, int kind);

  /// <summary>Add a reaction to an entity</summary>
  /// <remarks>Add a reaction to an entity. Will do nothing if the reaction already exists.</remarks>
  /// <param name="id">Entity ID</param>
  /// <param name="kind">The kind of the entity. Can be either `tasks` or `comments` for task comments</param>
  /// <param name="project">The reaction you want to add to the entity.</param>
  /// <returns>The created reaction</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>403</term>
  /// <description>The user does not have access to the entity</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Put("/{kind}/{id}/reactions")]
  Task<Reaction> ReactionsPut(int id, int kind, [Body] Reaction project);

  /// <summary>Removes the user's reaction</summary>
  /// <remarks>Removes the reaction of that user on that entity.</remarks>
  /// <param name="id">Entity ID</param>
  /// <param name="kind">The kind of the entity. Can be either `tasks` or `comments` for task comments</param>
  /// <param name="project">The reaction you want to add to the entity.</param>
  /// <returns>The reaction was successfully removed.</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>403</term>
  /// <description>The user does not have access to the entity</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Post("/{kind}/{id}/reactions/delete")]
  Task<Message> ReactionsDelete(int id, int kind, [Body] Reaction project);

  /// <summary>User Avatar</summary>
  /// <remarks>Returns the user avatar as image.</remarks>
  /// <param name="username">The username of the user who's avatar you want to get</param>
  /// <param name="size">The size of the avatar you want to get. If bigger than the max configured size this will be adjusted to the maximum size.</param>
  /// <returns>The avatar</returns>
  /// <exception cref="ApiException">
  /// Thrown when the request returns a non-success status code:
  /// <list type="table">
  /// <listheader>
  /// <term>Status</term>
  /// <description>Description</description>
  /// </listheader>
  /// <item>
  /// <term>404</term>
  /// <description>The user does not exist.</description>
  /// </item>
  /// <item>
  /// <term>500</term>
  /// <description>Internal error</description>
  /// </item>
  /// </list>
  /// </exception>
  [Get("/{username}/avatar")]
  Task<FileResponse> Avatar(string username, [Query] int? size = default);
}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class Token
{

  [JsonPropertyName("token")]
  public string Token1 { get; set; } = "";

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class Image
{

  [JsonPropertyName("blur_hash")]
  public string BlurHash { get; set; } = "";

  [JsonPropertyName("id")]
  public string Id { get; set; } = "";

  /// <summary>
  /// This can be used to supply extra information from an image provider to clients
  /// </summary>
  [JsonPropertyName("info")]
  public object Info { get; set; }

  [JsonPropertyName("thumb")]
  public string Thumb { get; set; } = "";

  [JsonPropertyName("url")]
  public string Url { get; set; } = "";

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class Provider
{

  [JsonPropertyName("auth_url")]
  public string AuthUrl { get; set; } = "";

  [JsonPropertyName("client_id")]
  public string ClientId { get; set; } = "";

  [JsonPropertyName("email_fallback")]
  public bool? EmailFallback { get; set; }

  [JsonPropertyName("force_user_info")]
  public bool? ForceUserInfo { get; set; }

  [JsonPropertyName("key")]
  public string Key { get; set; } = "";

  [JsonPropertyName("logout_url")]
  public string LogoutUrl { get; set; } = "";

  [JsonPropertyName("name")]
  public string Name { get; set; } = "";

  [JsonPropertyName("scope")]
  public string Scope { get; set; } = "";

  [JsonPropertyName("username_fallback")]
  public bool? UsernameFallback { get; set; }

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class File
{

  [JsonPropertyName("created")]
  public DateTimeOffset? Created { get; set; }

  [JsonPropertyName("id")]
  public int Id { get; set; }

  [JsonPropertyName("mime")]
  public string Mime { get; set; } = "";

  [JsonPropertyName("name")]
  public string Name { get; set; } = "";

  [JsonPropertyName("size")]
  public int? Size { get; set; }

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class AuthURL
{

  [JsonPropertyName("url")]
  public string Url { get; set; } = "";

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class Migration
{

  [JsonPropertyName("code")]
  public string Code { get; set; } = "";

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class Status
{

  [JsonPropertyName("finished_at")]
  public string FinishedAt { get; set; } = "";

  [JsonPropertyName("id")]
  public int Id { get; set; }

  [JsonPropertyName("migrator_name")]
  public string MigratorName { get; set; } = "";

  [JsonPropertyName("started_at")]
  public string StartedAt { get; set; } = "";

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class APIPermissions : Dictionary<string, System.Collections.ObjectModel.Collection<string>>
{

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class APIToken
{

  /// <summary>
  /// A timestamp when this api key was created. You cannot change this value.
  /// </summary>
  [JsonPropertyName("created")]
  public DateTimeOffset? Created { get; set; }

  /// <summary>
  /// The date when this key expires.
  /// </summary>
  [JsonPropertyName("expires_at")]
  public string ExpiresAt { get; set; } = "";

  /// <summary>
  /// The unique, numeric id of this api key.
  /// </summary>
  [JsonPropertyName("id")]
  public int Id { get; set; }

  /// <summary>
  /// The permissions this token has. Possible values are available via the /routes endpoint and consist of the keys of the list from that endpoint. For example, if the token should be able to read all tasks as well as update existing tasks, you should add `{"tasks":["read_all","update"]}`.
  /// </summary>
  [JsonPropertyName("permissions")]
  public APIPermissions Permissions { get; set; }

  /// <summary>
  /// A human-readable name for this token
  /// </summary>
  [JsonPropertyName("title")]
  public string Title { get; set; } = "";

  /// <summary>
  /// The actual api key. Only visible after creation.
  /// </summary>
  [JsonPropertyName("token")]
  public string Token { get; set; } = "";

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class APITokenRoute : Dictionary<string, RouteDetail>
{

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class Bucket
{

  /// <summary>
  /// The number of tasks currently in this bucket
  /// </summary>
  [JsonPropertyName("count")]
  public int? Count { get; set; }

  /// <summary>
  /// A timestamp when this bucket was created. You cannot change this value.
  /// </summary>
  [JsonPropertyName("created")]
  public DateTimeOffset? Created { get; set; }

  /// <summary>
  /// The user who initially created the bucket.
  /// </summary>
  [JsonPropertyName("created_by")]
  public User CreatedBy { get; set; }

  /// <summary>
  /// The unique, numeric id of this bucket.
  /// </summary>
  [JsonPropertyName("id")]
  public int Id { get; set; }

  /// <summary>
  /// How many tasks can be at the same time on this board max
  /// </summary>
  [JsonPropertyName("limit")]
  [System.ComponentModel.DataAnnotations.Range(0, int.MaxValue)]
  public int? Limit { get; set; }

  /// <summary>
  /// The position this bucket has when querying all buckets. See the tasks.position property on how to use this.
  /// </summary>
  [JsonPropertyName("position")]
  public double? Position { get; set; }

  /// <summary>
  /// The project view this bucket belongs to.
  /// </summary>
  [JsonPropertyName("project_view_id")]
  public int? ProjectViewId { get; set; }

  /// <summary>
  /// All tasks which belong to this bucket.
  /// </summary>
  [JsonPropertyName("tasks")]
  public ICollection<Task> Tasks { get; set; } = new List<Task>();

  /// <summary>
  /// The title of this bucket.
  /// </summary>
  [JsonPropertyName("title")]
  [System.ComponentModel.DataAnnotations.StringLength(int.MaxValue, MinimumLength = 1)]
  public string Title { get; set; } = "";

  /// <summary>
  /// A timestamp when this bucket was last updated. You cannot change this value.
  /// </summary>
  [JsonPropertyName("updated")]
  public DateTimeOffset? Updated { get; set; }

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class BulkAssignees
{

  /// <summary>
  /// A project with all assignees
  /// </summary>
  [JsonPropertyName("assignees")]
  public ICollection<User> Assignees { get; set; } = new List<User>();

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class BulkTask
{

  [JsonPropertyName("fields")]
  public ICollection<string> Fields { get; set; } = new List<string>();

  [JsonPropertyName("task_ids")]
  public ICollection<int> TaskIds { get; set; } = new List<int>();

  [JsonPropertyName("tasks")]
  public ICollection<Task> Tasks { get; set; } = new List<Task>();

  [JsonPropertyName("values")]
  public Task Values { get; set; }

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class DatabaseNotifications
{

  /// <summary>
  /// A timestamp when this notification was created. You cannot change this value.
  /// </summary>
  [JsonPropertyName("created")]
  public DateTimeOffset? Created { get; set; }

  /// <summary>
  /// The unique, numeric id of this notification.
  /// </summary>
  [JsonPropertyName("id")]
  public int Id { get; set; }

  /// <summary>
  /// The name of the notification
  /// </summary>
  [JsonPropertyName("name")]
  public string Name { get; set; } = "";

  /// <summary>
  /// The actual content of the notification.
  /// </summary>
  [JsonPropertyName("notification")]
  public object Notification { get; set; }

  /// <summary>
  /// Whether or not to mark this notification as read or unread.
  /// <br/>True is read, false is unread.
  /// </summary>
  [JsonPropertyName("read")]
  public bool? Read { get; set; }

  /// <summary>
  /// When this notification is marked as read, this will be updated with the current timestamp.
  /// </summary>
  [JsonPropertyName("read_at")]
  public string ReadAt { get; set; } = "";

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class Label
{

  /// <summary>
  /// A timestamp when this label was created. You cannot change this value.
  /// </summary>
  [JsonPropertyName("created")]
  public DateTimeOffset? Created { get; set; }

  /// <summary>
  /// The user who created this label
  /// </summary>
  [JsonPropertyName("created_by")]
  public User CreatedBy { get; set; }

  /// <summary>
  /// The label description.
  /// </summary>
  [JsonPropertyName("description")]
  public string Description { get; set; } = "";

  /// <summary>
  /// The color this label has in hex format.
  /// </summary>
  [JsonPropertyName("hex_color")]
  [System.ComponentModel.DataAnnotations.StringLength(7)]
  public string HexColor { get; set; } = "";

  /// <summary>
  /// The unique, numeric id of this label.
  /// </summary>
  [JsonPropertyName("id")]
  public int Id { get; set; }

  /// <summary>
  /// The title of the label. You'll see this one on tasks associated with it.
  /// </summary>
  [JsonPropertyName("title")]
  [System.ComponentModel.DataAnnotations.StringLength(250, MinimumLength = 1)]
  public string Title { get; set; } = "";

  /// <summary>
  /// A timestamp when this label was last updated. You cannot change this value.
  /// </summary>
  [JsonPropertyName("updated")]
  public DateTimeOffset? Updated { get; set; }

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class LabelTask
{

  /// <summary>
  /// A timestamp when this task was created. You cannot change this value.
  /// </summary>
  [JsonPropertyName("created")]
  public DateTimeOffset? Created { get; set; }

  /// <summary>
  /// The label id you want to associate with a task.
  /// </summary>
  [JsonPropertyName("label_id")]
  public int? LabelId { get; set; }

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class LabelTaskBulk
{

  /// <summary>
  /// All labels you want to update at once.
  /// </summary>
  [JsonPropertyName("labels")]
  public ICollection<Label> Labels { get; set; } = new List<Label>();

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class LinkSharing
{

  /// <summary>
  /// A timestamp when this project was shared. You cannot change this value.
  /// </summary>
  [JsonPropertyName("created")]
  public DateTimeOffset? Created { get; set; }

  /// <summary>
  /// The public id to get this shared project
  /// </summary>
  [JsonPropertyName("hash")]
  public string Hash { get; set; } = "";

  /// <summary>
  /// The ID of the shared thing
  /// </summary>
  [JsonPropertyName("id")]
  public int Id { get; set; }

  /// <summary>
  /// The name of this link share. All actions someone takes while being authenticated with that link will appear with that name.
  /// </summary>
  [JsonPropertyName("name")]
  public string Name { get; set; } = "";

  /// <summary>
  /// The password of this link share. You can only set it, not retrieve it after the link share has been created.
  /// </summary>
  [JsonPropertyName("password")]
  public string Password { get; set; } = "";

  /// <summary>
  /// The permission this project is shared with. 0 = Read only, 1 = Read &amp; Write, 2 = Admin. See the docs for more details.
  /// </summary>
  [JsonPropertyName("permission")]
  public Permission? Permission { get; set; } = global::Permission._0;

  /// <summary>
  /// The user who shared this project
  /// </summary>
  [JsonPropertyName("shared_by")]
  public User SharedBy { get; set; }

  /// <summary>
  /// The kind of this link. 0 = undefined, 1 = without password, 2 = with password.
  /// </summary>
  [JsonPropertyName("sharing_type")]
  public SharingType? SharingType { get; set; } = global::SharingType._0;

  /// <summary>
  /// A timestamp when this share was last updated. You cannot change this value.
  /// </summary>
  [JsonPropertyName("updated")]
  public DateTimeOffset? Updated { get; set; }

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class Message
{

  /// <summary>
  /// A standard message.
  /// </summary>
  [JsonPropertyName("message")]
  public string Message1 { get; set; } = "";

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public enum Permission
{

  _0 = 0,

  _1 = 1,

  _2 = 2,

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class Project
{

  /// <summary>
  /// Contains a very small version of the project background to use as a blurry preview until the actual background is loaded. Check out https://blurha.sh/ to learn how it works.
  /// </summary>
  [JsonPropertyName("background_blur_hash")]
  public string BackgroundBlurHash { get; set; } = "";

  /// <summary>
  /// Holds extra information about the background set since some background providers require attribution or similar. If not null, the background can be accessed at /projects/{projectID}/background
  /// </summary>
  [JsonPropertyName("background_information")]
  public object BackgroundInformation { get; set; }

  /// <summary>
  /// A timestamp when this project was created. You cannot change this value.
  /// </summary>
  [JsonPropertyName("created")]
  public DateTimeOffset? Created { get; set; }

  /// <summary>
  /// The description of the project.
  /// </summary>
  [JsonPropertyName("description")]
  public string Description { get; set; } = "";

  /// <summary>
  /// The hex color of this project
  /// </summary>
  [JsonPropertyName("hex_color")]
  [System.ComponentModel.DataAnnotations.StringLength(7)]
  public string HexColor { get; set; } = "";

  /// <summary>
  /// The unique, numeric id of this project.
  /// </summary>
  [JsonPropertyName("id")]
  public int Id { get; set; }

  /// <summary>
  /// The unique project short identifier. Used to build task identifiers.
  /// </summary>
  [JsonPropertyName("identifier")]
  [System.ComponentModel.DataAnnotations.StringLength(10)]
  public string Identifier { get; set; } = "";

  /// <summary>
  /// Whether a project is archived.
  /// </summary>
  [JsonPropertyName("is_archived")]
  public bool? IsArchived { get; set; }

  /// <summary>
  /// True if a project is a favorite. Favorite projects show up in a separate parent project. This value depends on the user making the call to the api.
  /// </summary>
  [JsonPropertyName("is_favorite")]
  public bool? IsFavorite { get; set; }

  [JsonPropertyName("max_permission")]
  public Permission? MaxPermission { get; set; }

  /// <summary>
  /// The user who created this project.
  /// </summary>
  [JsonPropertyName("owner")]
  public User Owner { get; set; }

  [JsonPropertyName("parent_project_id")]
  public int? ParentProjectId { get; set; }

  /// <summary>
  /// The position this project has when querying all projects. See the tasks.position property on how to use this.
  /// </summary>
  [JsonPropertyName("position")]
  public double? Position { get; set; }

  /// <summary>
  /// The subscription status for the user reading this project. You can only read this property, use the subscription endpoints to modify it.
  /// <br/>Will only returned when retreiving one project.
  /// </summary>
  [JsonPropertyName("subscription")]
  public Subscription Subscription { get; set; }

  /// <summary>
  /// The title of the project. You'll see this in the overview.
  /// </summary>
  [JsonPropertyName("title")]
  [System.ComponentModel.DataAnnotations.StringLength(250, MinimumLength = 1)]
  public string Title { get; set; } = "";

  /// <summary>
  /// A timestamp when this project was last updated. You cannot change this value.
  /// </summary>
  [JsonPropertyName("updated")]
  public DateTimeOffset? Updated { get; set; }

  [JsonPropertyName("views")]
  public ICollection<ProjectView> Views { get; set; } = new List<ProjectView>();

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class ProjectDuplicate
{

  /// <summary>
  /// The copied project
  /// </summary>
  [JsonPropertyName("duplicated_project")]
  public Project DuplicatedProject { get; set; }

  /// <summary>
  /// The target parent project
  /// </summary>
  [JsonPropertyName("parent_project_id")]
  public int? ParentProjectId { get; set; }

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class ProjectUser
{

  /// <summary>
  /// A timestamp when this relation was created. You cannot change this value.
  /// </summary>
  [JsonPropertyName("created")]
  public DateTimeOffset? Created { get; set; }

  /// <summary>
  /// The unique, numeric id of this project &lt;-&gt; user relation.
  /// </summary>
  [JsonPropertyName("id")]
  public int Id { get; set; }

  /// <summary>
  /// The permission this user has. 0 = Read only, 1 = Read &amp; Write, 2 = Admin. See the docs for more details.
  /// </summary>
  [JsonPropertyName("permission")]
  public Permission? Permission { get; set; } = global::Permission._0;

  /// <summary>
  /// A timestamp when this relation was last updated. You cannot change this value.
  /// </summary>
  [JsonPropertyName("updated")]
  public DateTimeOffset? Updated { get; set; }

  /// <summary>
  /// The username.
  /// </summary>
  [JsonPropertyName("username")]
  public string Username { get; set; } = "";

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class ProjectView
{

  /// <summary>
  /// When the bucket configuration mode is not `manual`, this field holds the options of that configuration.
  /// </summary>
  [JsonPropertyName("bucket_configuration")]
  public ICollection<ProjectViewBucketConfiguration> BucketConfiguration { get; set; } = new List<ProjectViewBucketConfiguration>();

  /// <summary>
  /// The bucket configuration mode. Can be `none`, `manual` or `filter`. `manual` allows to move tasks between buckets as you normally would. `filter` creates buckets based on a filter for each bucket.
  /// </summary>
  [JsonPropertyName("bucket_configuration_mode")]
  [JsonConverter(typeof(JsonStringEnumConverter<ProjectViewBucketConfigurationMode>))]
  public ProjectViewBucketConfigurationMode? BucketConfigurationMode { get; set; }

  /// <summary>
  /// A timestamp when this reaction was created. You cannot change this value.
  /// </summary>
  [JsonPropertyName("created")]
  public DateTimeOffset? Created { get; set; }

  /// <summary>
  /// The ID of the bucket where new tasks without a bucket are added to. By default, this is the leftmost bucket in a view.
  /// </summary>
  [JsonPropertyName("default_bucket_id")]
  public int? DefaultBucketId { get; set; }

  /// <summary>
  /// If tasks are moved to the done bucket, they are marked as done. If they are marked as done individually, they are moved into the done bucket.
  /// </summary>
  [JsonPropertyName("done_bucket_id")]
  public int? DoneBucketId { get; set; }

  /// <summary>
  /// The filter query to match tasks by. Check out https://vikunja.io/docs/filters for a full explanation.
  /// </summary>
  [JsonPropertyName("filter")]
  public TaskCollection Filter { get; set; }

  /// <summary>
  /// The unique numeric id of this view
  /// </summary>
  [JsonPropertyName("id")]
  public int Id { get; set; }

  /// <summary>
  /// The position of this view in the list. The list of all views will be sorted by this parameter.
  /// </summary>
  [JsonPropertyName("position")]
  public double? Position { get; set; }

  /// <summary>
  /// The project this view belongs to
  /// </summary>
  [JsonPropertyName("project_id")]
  public int? ProjectId { get; set; }

  /// <summary>
  /// The title of this view
  /// </summary>
  [JsonPropertyName("title")]
  public string Title { get; set; } = "";

  /// <summary>
  /// A timestamp when this view was updated. You cannot change this value.
  /// </summary>
  [JsonPropertyName("updated")]
  public DateTimeOffset? Updated { get; set; }

  /// <summary>
  /// The kind of this view. Can be `list`, `gantt`, `table` or `kanban`.
  /// </summary>
  [JsonPropertyName("view_kind")]
  [JsonConverter(typeof(JsonStringEnumConverter<ProjectViewViewKind>))]
  public ProjectViewViewKind? ViewKind { get; set; }

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class ProjectViewBucketConfiguration
{

  [JsonPropertyName("filter")]
  public TaskCollection Filter { get; set; }

  [JsonPropertyName("title")]
  public string Title { get; set; } = "";

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class Reaction
{

  /// <summary>
  /// A timestamp when this reaction was created. You cannot change this value.
  /// </summary>
  [JsonPropertyName("created")]
  public DateTimeOffset? Created { get; set; }

  /// <summary>
  /// The user who reacted
  /// </summary>
  [JsonPropertyName("user")]
  public User User { get; set; }

  /// <summary>
  /// The actual reaction. This can be any valid utf character or text, up to a length of 20.
  /// </summary>
  [JsonPropertyName("value")]
  public string Value { get; set; } = "";

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class ReactionMap : Dictionary<string, System.Collections.ObjectModel.Collection<User>>
{

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class RelatedTaskMap : Dictionary<string, System.Collections.ObjectModel.Collection<Task>>
{

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public enum RelationKind
{

  [System.Runtime.Serialization.EnumMember(Value = @"unknown")]
  Unknown = 0,

  [System.Runtime.Serialization.EnumMember(Value = @"subtask")]
  Subtask = 1,

  [System.Runtime.Serialization.EnumMember(Value = @"parenttask")]
  Parenttask = 2,

  [System.Runtime.Serialization.EnumMember(Value = @"related")]
  Related = 3,

  [System.Runtime.Serialization.EnumMember(Value = @"duplicateof")]
  Duplicateof = 4,

  [System.Runtime.Serialization.EnumMember(Value = @"duplicates")]
  Duplicates = 5,

  [System.Runtime.Serialization.EnumMember(Value = @"blocking")]
  Blocking = 6,

  [System.Runtime.Serialization.EnumMember(Value = @"blocked")]
  Blocked = 7,

  [System.Runtime.Serialization.EnumMember(Value = @"precedes")]
  Precedes = 8,

  [System.Runtime.Serialization.EnumMember(Value = @"follows")]
  Follows = 9,

  [System.Runtime.Serialization.EnumMember(Value = @"copiedfrom")]
  Copiedfrom = 10,

  [System.Runtime.Serialization.EnumMember(Value = @"copiedto")]
  Copiedto = 11,

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public enum ReminderRelation
{

  [System.Runtime.Serialization.EnumMember(Value = @"due_date")]
  Due_date = 0,

  [System.Runtime.Serialization.EnumMember(Value = @"start_date")]
  Start_date = 1,

  [System.Runtime.Serialization.EnumMember(Value = @"end_date")]
  End_date = 2,

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class RouteDetail
{

  [JsonPropertyName("method")]
  public string Method { get; set; } = "";

  [JsonPropertyName("path")]
  public string Path { get; set; } = "";

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class SavedFilter
{

  /// <summary>
  /// A timestamp when this filter was created. You cannot change this value.
  /// </summary>
  [JsonPropertyName("created")]
  public DateTimeOffset? Created { get; set; }

  /// <summary>
  /// The description of the filter
  /// </summary>
  [JsonPropertyName("description")]
  public string Description { get; set; } = "";

  /// <summary>
  /// The actual filters this filter contains
  /// </summary>
  [JsonPropertyName("filters")]
  public TaskCollection Filters { get; set; }

  /// <summary>
  /// The unique numeric id of this saved filter
  /// </summary>
  [JsonPropertyName("id")]
  public int Id { get; set; }

  /// <summary>
  /// True if the filter is a favorite. Favorite filters show up in a separate parent project together with favorite projects.
  /// </summary>
  [JsonPropertyName("is_favorite")]
  public bool? IsFavorite { get; set; }

  /// <summary>
  /// The user who owns this filter
  /// </summary>
  [JsonPropertyName("owner")]
  public User Owner { get; set; }

  /// <summary>
  /// The title of the filter.
  /// </summary>
  [JsonPropertyName("title")]
  [System.ComponentModel.DataAnnotations.StringLength(250, MinimumLength = 1)]
  public string Title { get; set; } = "";

  /// <summary>
  /// A timestamp when this filter was last updated. You cannot change this value.
  /// </summary>
  [JsonPropertyName("updated")]
  public DateTimeOffset? Updated { get; set; }

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public enum SharingType
{

  _0 = 0,

  _1 = 1,

  _2 = 2,

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class Subscription
{

  /// <summary>
  /// A timestamp when this subscription was created. You cannot change this value.
  /// </summary>
  [JsonPropertyName("created")]
  public DateTimeOffset? Created { get; set; }

  [JsonPropertyName("entity")]
  public int? Entity { get; set; }

  /// <summary>
  /// The id of the entity to subscribe to.
  /// </summary>
  [JsonPropertyName("entity_id")]
  public int? EntityId { get; set; }

  /// <summary>
  /// The numeric ID of the subscription
  /// </summary>
  [JsonPropertyName("id")]
  public int Id { get; set; }

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class Task
{

  /// <summary>
  /// An array of users who are assigned to this task
  /// </summary>
  [JsonPropertyName("assignees")]
  public ICollection<User> Assignees { get; set; } = new List<User>();

  /// <summary>
  /// All attachments this task has. This property is read-onlym, you must use the separate endpoint to add attachments to a task.
  /// </summary>
  [JsonPropertyName("attachments")]
  public ICollection<TaskAttachment> Attachments { get; set; } = new List<TaskAttachment>();

  /// <summary>
  /// The bucket id. Will only be populated when the task is accessed via a view with buckets.
  /// <br/>Can be used to move a task between buckets. In that case, the new bucket must be in the same view as the old one.
  /// </summary>
  [JsonPropertyName("bucket_id")]
  public int? BucketId { get; set; }

  /// <summary>
  /// All buckets across all views this task is part of. Only present when fetching tasks with the `expand` parameter set to `buckets`.
  /// </summary>
  [JsonPropertyName("buckets")]
  public ICollection<Bucket> Buckets { get; set; } = new List<Bucket>();

  /// <summary>
  /// Comment count of this task. Only present when fetching tasks with the `expand` parameter set to `comment_count`.
  /// </summary>
  [JsonPropertyName("comment_count")]
  public int? CommentCount { get; set; }

  /// <summary>
  /// All comments of this task. Only present when fetching tasks with the `expand` parameter set to `comments`.
  /// </summary>
  [JsonPropertyName("comments")]
  public ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();

  /// <summary>
  /// If this task has a cover image, the field will return the id of the attachment that is the cover image.
  /// </summary>
  [JsonPropertyName("cover_image_attachment_id")]
  public int? CoverImageAttachmentId { get; set; }

  /// <summary>
  /// A timestamp when this task was created. You cannot change this value.
  /// </summary>
  [JsonPropertyName("created")]
  public DateTimeOffset? Created { get; set; }

  /// <summary>
  /// The user who initially created the task.
  /// </summary>
  [JsonPropertyName("created_by")]
  public User CreatedBy { get; set; }

  /// <summary>
  /// The task description.
  /// </summary>
  [JsonPropertyName("description")]
  public string Description { get; set; } = "";

  /// <summary>
  /// Whether a task is done or not.
  /// </summary>
  [JsonPropertyName("done")]
  public bool? Done { get; set; }

  /// <summary>
  /// The time when a task was marked as done. This field is system-controlled and cannot be set via API.
  /// </summary>
  [JsonPropertyName("done_at")]
  public DateTimeOffset? DoneAt { get; set; }

  /// <summary>
  /// The time when the task is due.
  /// </summary>
  [JsonPropertyName("due_date")]
  public DateTimeOffset? DueDate { get; set; }

  /// <summary>
  /// When this task ends.
  /// </summary>
  [JsonPropertyName("end_date")]
  public DateTimeOffset? EndDate { get; set; }

  /// <summary>
  /// The task color in hex
  /// </summary>
  [JsonPropertyName("hex_color")]
  [System.ComponentModel.DataAnnotations.StringLength(7)]
  public string HexColor { get; set; } = "";

  /// <summary>
  /// The unique, numeric id of this task.
  /// </summary>
  [JsonPropertyName("id")]
  public int Id { get; set; }

  /// <summary>
  /// The task identifier, based on the project identifier and the task's index
  /// </summary>
  [JsonPropertyName("identifier")]
  public string Identifier { get; set; } = "";

  /// <summary>
  /// The task index, calculated per project
  /// </summary>
  [JsonPropertyName("index")]
  public int? Index { get; set; }

  /// <summary>
  /// True if a task is a favorite task. Favorite tasks show up in a separate "Important" project. This value depends on the user making the call to the api.
  /// </summary>
  [JsonPropertyName("is_favorite")]
  public bool? IsFavorite { get; set; }

  [JsonPropertyName("is_unread")]
  public bool? IsUnread { get; set; }

  /// <summary>
  /// An array of labels which are associated with this task. This property is read-only, you must use the separate endpoint to add labels to a task.
  /// </summary>
  [JsonPropertyName("labels")]
  public ICollection<Label> Labels { get; set; } = new List<Label>();

  /// <summary>
  /// Determines how far a task is left from being done
  /// </summary>
  [JsonPropertyName("percent_done")]
  public double? PercentDone { get; set; }

  /// <summary>
  /// The position of the task - any task project can be sorted as usual by this parameter.
  /// <br/>When accessing tasks via views with buckets, this is primarily used to sort them based on a range.
  /// <br/>Positions are always saved per view. They will automatically be set if you request the tasks through a view
  /// <br/>endpoint, otherwise they will always be 0. To update them, take a look at the Task Position endpoint.
  /// </summary>
  [JsonPropertyName("position")]
  public double? Position { get; set; }

  /// <summary>
  /// The task priority. Can be anything you want, it is possible to sort by this later.
  /// </summary>
  [JsonPropertyName("priority")]
  public int? Priority { get; set; }

  /// <summary>
  /// The project this task belongs to.
  /// </summary>
  [JsonPropertyName("project_id")]
  public int? ProjectId { get; set; }

  /// <summary>
  /// Reactions on that task.
  /// </summary>
  [JsonPropertyName("reactions")]
  public ReactionMap Reactions { get; set; }

  /// <summary>
  /// All related tasks, grouped by their relation kind
  /// </summary>
  [JsonPropertyName("related_tasks")]
  public RelatedTaskMap RelatedTasks { get; set; }

  /// <summary>
  /// An array of reminders that are associated with this task.
  /// </summary>
  [JsonPropertyName("reminders")]
  public ICollection<TaskReminder> Reminders { get; set; } = new List<TaskReminder>();

  /// <summary>
  /// An amount in seconds this task repeats itself. If this is set, when marking the task as done, it will mark itself as "undone" and then increase all remindes and the due date by its amount.
  /// </summary>
  [JsonPropertyName("repeat_after")]
  public int? RepeatAfter { get; set; }

  /// <summary>
  /// Can have three possible values which will trigger when the task is marked as done: 0 = repeats after the amount specified in repeat_after, 1 = repeats all dates each months (ignoring repeat_after), 3 = repeats from the current date rather than the last set date.
  /// </summary>
  [JsonPropertyName("repeat_mode")]
  public TaskRepeatMode? RepeatMode { get; set; }

  /// <summary>
  /// When this task starts.
  /// </summary>
  [JsonPropertyName("start_date")]
  public DateTimeOffset? StartDate { get; set; }

  /// <summary>
  /// The subscription status for the user reading this task. You can only read this property, use the subscription endpoints to modify it.
  /// <br/>Will only returned when retrieving one task.
  /// </summary>
  [JsonPropertyName("subscription")]
  public Subscription Subscription { get; set; }

  /// <summary>
  /// The task text. This is what you'll see in the project.
  /// </summary>
  [JsonPropertyName("title")]
  [System.ComponentModel.DataAnnotations.StringLength(int.MaxValue, MinimumLength = 1)]
  public string Title { get; set; } = "";

  /// <summary>
  /// A timestamp when this task was last updated. You cannot change this value.
  /// </summary>
  [JsonPropertyName("updated")]
  public DateTimeOffset? Updated { get; set; }

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class TaskAssginee
{

  [JsonPropertyName("created")]
  public DateTimeOffset? Created { get; set; }

  [JsonPropertyName("user_id")]
  public int? UserId { get; set; }

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class TaskAttachment
{

  [JsonPropertyName("created")]
  public DateTimeOffset? Created { get; set; }

  [JsonPropertyName("created_by")]
  public User CreatedBy { get; set; }

  [JsonPropertyName("file")]
  public File File { get; set; }

  [JsonPropertyName("id")]
  public int Id { get; set; }

  [JsonPropertyName("task_id")]
  public int? TaskId { get; set; }

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class TaskBucket
{

  [JsonPropertyName("bucket")]
  public Bucket Bucket { get; set; }

  [JsonPropertyName("bucket_id")]
  public int? BucketId { get; set; }

  /// <summary>
  /// The view this bucket belongs to. Combined with TaskID this forms a
  /// <br/>unique index.
  /// </summary>
  [JsonPropertyName("project_view_id")]
  public int? ProjectViewId { get; set; }

  [JsonPropertyName("task")]
  public Task Task { get; set; }

  /// <summary>
  /// The task which belongs to the bucket. Together with ProjectViewID
  /// <br/>this field is part of a unique index to prevent duplicates.
  /// </summary>
  [JsonPropertyName("task_id")]
  public int? TaskId { get; set; }

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class TaskCollection
{

  /// <summary>
  /// The filter query to match tasks by. Check out https://vikunja.io/docs/filters for a full explanation.
  /// </summary>
  [JsonPropertyName("filter")]
  public string Filter { get; set; } = "";

  /// <summary>
  /// If set to true, the result will also include null values
  /// </summary>
  [JsonPropertyName("filter_include_nulls")]
  public bool? FilterIncludeNulls { get; set; }

  /// <summary>
  /// The query parameter to order the items by. This can be either asc or desc, with asc being the default.
  /// </summary>
  [JsonPropertyName("order_by")]
  public ICollection<string> OrderBy { get; set; } = new List<string>();

  [JsonPropertyName("s")]
  public string S { get; set; } = "";

  /// <summary>
  /// The query parameter to sort by. This is for ex. done, priority, etc.
  /// </summary>
  [JsonPropertyName("sort_by")]
  public ICollection<string> SortBy { get; set; } = new List<string>();

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class TaskComment
{

  [JsonPropertyName("author")]
  public User Author { get; set; }

  [JsonPropertyName("comment")]
  public string Comment { get; set; } = "";

  [JsonPropertyName("created")]
  public DateTimeOffset? Created { get; set; }

  [JsonPropertyName("id")]
  public int Id { get; set; }

  [JsonPropertyName("reactions")]
  public ReactionMap Reactions { get; set; }

  [JsonPropertyName("updated")]
  public DateTimeOffset? Updated { get; set; }

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class TaskPosition
{

  /// <summary>
  /// The position of the task - any task project can be sorted as usual by this parameter.
  /// <br/>When accessing tasks via kanban buckets, this is primarily used to sort them based on a range
  /// <br/>We're using a float64 here to make it possible to put any task within any two other tasks (by changing the number).
  /// <br/>You would calculate the new position between two tasks with something like task3.position = (task2.position - task1.position) / 2.
  /// <br/>A 64-Bit float leaves plenty of room to initially give tasks a position with 2^16 difference to the previous task
  /// <br/>which also leaves a lot of room for rearranging and sorting later.
  /// <br/>Positions are always saved per view. They will automatically be set if you request the tasks through a view
  /// <br/>endpoint, otherwise they will always be 0. To update them, take a look at the Task Position endpoint.
  /// </summary>
  [JsonPropertyName("position")]
  public double? Position { get; set; }

  /// <summary>
  /// The project view this task is related to
  /// </summary>
  [JsonPropertyName("project_view_id")]
  public int? ProjectViewId { get; set; }

  /// <summary>
  /// The ID of the task this position is for
  /// </summary>
  [JsonPropertyName("task_id")]
  public int? TaskId { get; set; }

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class TaskRelation
{

  /// <summary>
  /// A timestamp when this label was created. You cannot change this value.
  /// </summary>
  [JsonPropertyName("created")]
  public DateTimeOffset? Created { get; set; }

  /// <summary>
  /// The user who created this relation
  /// </summary>
  [JsonPropertyName("created_by")]
  public User CreatedBy { get; set; }

  /// <summary>
  /// The ID of the other task, the task which is being related.
  /// </summary>
  [JsonPropertyName("other_task_id")]
  public int? OtherTaskId { get; set; }

  /// <summary>
  /// The kind of the relation.
  /// </summary>
  [JsonPropertyName("relation_kind")]
  [JsonConverter(typeof(JsonStringEnumConverter<RelationKind>))]
  public RelationKind? RelationKind { get; set; }

  /// <summary>
  /// The ID of the "base" task, the task which has a relation to another.
  /// </summary>
  [JsonPropertyName("task_id")]
  public int? TaskId { get; set; }

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class TaskReminder
{

  /// <summary>
  /// A period in seconds relative to another date argument. Negative values mean the reminder triggers before the date. Default: 0, tiggers when RelativeTo is due.
  /// </summary>
  [JsonPropertyName("relative_period")]
  public int? RelativePeriod { get; set; }

  /// <summary>
  /// The name of the date field to which the relative period refers to.
  /// </summary>
  [JsonPropertyName("relative_to")]
  [JsonConverter(typeof(JsonStringEnumConverter<ReminderRelation>))]
  public ReminderRelation? RelativeTo { get; set; }

  /// <summary>
  /// The absolute time when the user wants to be reminded of the task.
  /// </summary>
  [JsonPropertyName("reminder")]
  public string Reminder { get; set; } = "";

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public enum TaskRepeatMode
{

  _0 = 0,

  _1 = 1,

  _2 = 2,

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class TaskUnreadStatus
{

  [JsonPropertyName("taskID")]
  public int? TaskID { get; set; }

  [JsonPropertyName("userID")]
  public int? UserID { get; set; }

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class Team
{

  /// <summary>
  /// A timestamp when this relation was created. You cannot change this value.
  /// </summary>
  [JsonPropertyName("created")]
  public DateTimeOffset? Created { get; set; }

  /// <summary>
  /// The user who created this team.
  /// </summary>
  [JsonPropertyName("created_by")]
  public User CreatedBy { get; set; }

  /// <summary>
  /// The team's description.
  /// </summary>
  [JsonPropertyName("description")]
  public string Description { get; set; } = "";

  /// <summary>
  /// The team's external id provided by the openid or ldap provider
  /// </summary>
  [JsonPropertyName("external_id")]
  [System.ComponentModel.DataAnnotations.StringLength(250)]
  public string ExternalId { get; set; } = "";

  /// <summary>
  /// The unique, numeric id of this team.
  /// </summary>
  [JsonPropertyName("id")]
  public int Id { get; set; }

  /// <summary>
  /// Query parameter controlling whether to include public projects or not
  /// </summary>
  [JsonPropertyName("include_public")]
  public bool? IncludePublic { get; set; }

  /// <summary>
  /// Defines wether the team should be publicly discoverable when sharing a project
  /// </summary>
  [JsonPropertyName("is_public")]
  public bool? IsPublic { get; set; }

  /// <summary>
  /// An array of all members in this team.
  /// </summary>
  [JsonPropertyName("members")]
  public ICollection<TeamUser> Members { get; set; } = new List<TeamUser>();

  /// <summary>
  /// The name of this team.
  /// </summary>
  [JsonPropertyName("name")]
  [System.ComponentModel.DataAnnotations.StringLength(250, MinimumLength = 1)]
  public string Name { get; set; } = "";

  /// <summary>
  /// A timestamp when this relation was last updated. You cannot change this value.
  /// </summary>
  [JsonPropertyName("updated")]
  public DateTimeOffset? Updated { get; set; }

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class TeamMember
{

  /// <summary>
  /// Whether or not the member is an admin of the team. See the docs for more about what a team admin can do
  /// </summary>
  [JsonPropertyName("admin")]
  public bool? Admin { get; set; }

  /// <summary>
  /// A timestamp when this relation was created. You cannot change this value.
  /// </summary>
  [JsonPropertyName("created")]
  public DateTimeOffset? Created { get; set; }

  /// <summary>
  /// The unique, numeric id of this team member relation.
  /// </summary>
  [JsonPropertyName("id")]
  public int Id { get; set; }

  /// <summary>
  /// The username of the member. We use this to prevent automated user id entering.
  /// </summary>
  [JsonPropertyName("username")]
  public string Username { get; set; } = "";

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class TeamProject
{

  /// <summary>
  /// A timestamp when this relation was created. You cannot change this value.
  /// </summary>
  [JsonPropertyName("created")]
  public DateTimeOffset? Created { get; set; }

  /// <summary>
  /// The unique, numeric id of this project &lt;-&gt; team relation.
  /// </summary>
  [JsonPropertyName("id")]
  public int Id { get; set; }

  /// <summary>
  /// The permission this team has. 0 = Read only, 1 = Read &amp; Write, 2 = Admin. See the docs for more details.
  /// </summary>
  [JsonPropertyName("permission")]
  public Permission? Permission { get; set; } = global::Permission._0;

  /// <summary>
  /// The team id.
  /// </summary>
  [JsonPropertyName("team_id")]
  public int? TeamId { get; set; }

  /// <summary>
  /// A timestamp when this relation was last updated. You cannot change this value.
  /// </summary>
  [JsonPropertyName("updated")]
  public DateTimeOffset? Updated { get; set; }

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class TeamUser
{

  /// <summary>
  /// Whether the member is an admin of the team. See the docs for more about what a team admin can do
  /// </summary>
  [JsonPropertyName("admin")]
  public bool? Admin { get; set; }

  /// <summary>
  /// A timestamp when this task was created. You cannot change this value.
  /// </summary>
  [JsonPropertyName("created")]
  public DateTimeOffset? Created { get; set; }

  /// <summary>
  /// The user's email address.
  /// </summary>
  [JsonPropertyName("email")]
  [System.ComponentModel.DataAnnotations.StringLength(250)]
  public string Email { get; set; } = "";

  /// <summary>
  /// The unique, numeric id of this user.
  /// </summary>
  [JsonPropertyName("id")]
  public int Id { get; set; }

  /// <summary>
  /// The full name of the user.
  /// </summary>
  [JsonPropertyName("name")]
  public string Name { get; set; } = "";

  /// <summary>
  /// A timestamp when this task was last updated. You cannot change this value.
  /// </summary>
  [JsonPropertyName("updated")]
  public DateTimeOffset? Updated { get; set; }

  /// <summary>
  /// The username of the user. Is always unique.
  /// </summary>
  [JsonPropertyName("username")]
  [System.ComponentModel.DataAnnotations.StringLength(250, MinimumLength = 1)]
  public string Username { get; set; } = "";

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class TeamWithPermission
{

  /// <summary>
  /// A timestamp when this relation was created. You cannot change this value.
  /// </summary>
  [JsonPropertyName("created")]
  public DateTimeOffset? Created { get; set; }

  /// <summary>
  /// The user who created this team.
  /// </summary>
  [JsonPropertyName("created_by")]
  public User CreatedBy { get; set; }

  /// <summary>
  /// The team's description.
  /// </summary>
  [JsonPropertyName("description")]
  public string Description { get; set; } = "";

  /// <summary>
  /// The team's external id provided by the openid or ldap provider
  /// </summary>
  [JsonPropertyName("external_id")]
  [System.ComponentModel.DataAnnotations.StringLength(250)]
  public string ExternalId { get; set; } = "";

  /// <summary>
  /// The unique, numeric id of this team.
  /// </summary>
  [JsonPropertyName("id")]
  public int Id { get; set; }

  /// <summary>
  /// Query parameter controlling whether to include public projects or not
  /// </summary>
  [JsonPropertyName("include_public")]
  public bool? IncludePublic { get; set; }

  /// <summary>
  /// Defines wether the team should be publicly discoverable when sharing a project
  /// </summary>
  [JsonPropertyName("is_public")]
  public bool? IsPublic { get; set; }

  /// <summary>
  /// An array of all members in this team.
  /// </summary>
  [JsonPropertyName("members")]
  public ICollection<TeamUser> Members { get; set; } = new List<TeamUser>();

  /// <summary>
  /// The name of this team.
  /// </summary>
  [JsonPropertyName("name")]
  [System.ComponentModel.DataAnnotations.StringLength(250, MinimumLength = 1)]
  public string Name { get; set; } = "";

  [JsonPropertyName("permission")]
  public Permission? Permission { get; set; }

  /// <summary>
  /// A timestamp when this relation was last updated. You cannot change this value.
  /// </summary>
  [JsonPropertyName("updated")]
  public DateTimeOffset? Updated { get; set; }

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class UserWithPermission
{

  /// <summary>
  /// A timestamp when this task was created. You cannot change this value.
  /// </summary>
  [JsonPropertyName("created")]
  public DateTimeOffset? Created { get; set; }

  /// <summary>
  /// The user's email address.
  /// </summary>
  [JsonPropertyName("email")]
  [System.ComponentModel.DataAnnotations.StringLength(250)]
  public string Email { get; set; } = "";

  /// <summary>
  /// The unique, numeric id of this user.
  /// </summary>
  [JsonPropertyName("id")]
  public int Id { get; set; }

  /// <summary>
  /// The full name of the user.
  /// </summary>
  [JsonPropertyName("name")]
  public string Name { get; set; } = "";

  [JsonPropertyName("permission")]
  public Permission? Permission { get; set; }

  /// <summary>
  /// A timestamp when this task was last updated. You cannot change this value.
  /// </summary>
  [JsonPropertyName("updated")]
  public DateTimeOffset? Updated { get; set; }

  /// <summary>
  /// The username of the user. Is always unique.
  /// </summary>
  [JsonPropertyName("username")]
  [System.ComponentModel.DataAnnotations.StringLength(250, MinimumLength = 1)]
  public string Username { get; set; } = "";

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class Webhook
{

  /// <summary>
  /// A timestamp when this webhook target was created. You cannot change this value.
  /// </summary>
  [JsonPropertyName("created")]
  public DateTimeOffset? Created { get; set; }

  /// <summary>
  /// The user who initially created the webhook target.
  /// </summary>
  [JsonPropertyName("created_by")]
  public User CreatedBy { get; set; }

  /// <summary>
  /// The webhook events which should fire this webhook target
  /// </summary>
  [JsonPropertyName("events")]
  public ICollection<string> Events { get; set; } = new List<string>();

  /// <summary>
  /// The generated ID of this webhook target
  /// </summary>
  [JsonPropertyName("id")]
  public int Id { get; set; }

  /// <summary>
  /// The project ID of the project this webhook target belongs to
  /// </summary>
  [JsonPropertyName("project_id")]
  public int? ProjectId { get; set; }

  /// <summary>
  /// If provided, webhook requests will be signed using HMAC. Check out the docs about how to use this: https://vikunja.io/docs/webhooks/#signing
  /// </summary>
  [JsonPropertyName("secret")]
  public string Secret { get; set; } = "";

  /// <summary>
  /// The target URL where the POST request with the webhook payload will be made
  /// </summary>
  [JsonPropertyName("target_url")]
  public string TargetUrl { get; set; } = "";

  /// <summary>
  /// A timestamp when this webhook target was last updated. You cannot change this value.
  /// </summary>
  [JsonPropertyName("updated")]
  public DateTimeOffset? Updated { get; set; }

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class DatabaseNotification
{

  /// <summary>
  /// A timestamp when this notification was created. You cannot change this value.
  /// </summary>
  [JsonPropertyName("created")]
  public DateTimeOffset? Created { get; set; }

  /// <summary>
  /// The unique, numeric id of this notification.
  /// </summary>
  [JsonPropertyName("id")]
  public int Id { get; set; }

  /// <summary>
  /// The name of the notification
  /// </summary>
  [JsonPropertyName("name")]
  public string Name { get; set; } = "";

  /// <summary>
  /// The actual content of the notification.
  /// </summary>
  [JsonPropertyName("notification")]
  public object Notification { get; set; }

  /// <summary>
  /// When this notification is marked as read, this will be updated with the current timestamp.
  /// </summary>
  [JsonPropertyName("read_at")]
  public string ReadAt { get; set; } = "";

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class Callback
{

  [JsonPropertyName("code")]
  public string Code { get; set; } = "";

  [JsonPropertyName("redirect_url")]
  public string RedirectUrl { get; set; } = "";

  [JsonPropertyName("scope")]
  public string Scope { get; set; } = "";

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class Migration2
{

  [JsonPropertyName("code")]
  public string Code { get; set; } = "";

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class Migration3
{

  [JsonPropertyName("code")]
  public string Code { get; set; } = "";

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class EmailConfirm
{

  /// <summary>
  /// The email confirm token sent via email.
  /// </summary>
  [JsonPropertyName("token")]
  public string Token { get; set; } = "";

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class EmailUpdate
{

  /// <summary>
  /// The new email address. Needs to be a valid email address.
  /// </summary>
  [JsonPropertyName("new_email")]
  public string NewEmail { get; set; } = "";

  /// <summary>
  /// The password of the user for confirmation.
  /// </summary>
  [JsonPropertyName("password")]
  public string Password { get; set; } = "";

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class Login
{

  /// <summary>
  /// If true, the token returned will be valid a lot longer than default. Useful for "remember me" style logins.
  /// </summary>
  [JsonPropertyName("long_token")]
  public bool? LongToken { get; set; }

  /// <summary>
  /// The password for the user.
  /// </summary>
  [JsonPropertyName("password")]
  public string Password { get; set; } = "";

  /// <summary>
  /// The totp passcode of a user. Only needs to be provided when enabled.
  /// </summary>
  [JsonPropertyName("totp_passcode")]
  public string TotpPasscode { get; set; } = "";

  /// <summary>
  /// The username used to log in.
  /// </summary>
  [JsonPropertyName("username")]
  public string Username { get; set; } = "";

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class PasswordReset
{

  /// <summary>
  /// The new password for this user.
  /// </summary>
  [JsonPropertyName("new_password")]
  public string NewPassword { get; set; } = "";

  /// <summary>
  /// The previously issued reset token.
  /// </summary>
  [JsonPropertyName("token")]
  public string Token { get; set; } = "";

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class PasswordTokenRequest
{

  [JsonPropertyName("email")]
  [System.ComponentModel.DataAnnotations.StringLength(250)]
  public string Email { get; set; } = "";

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class TOTP
{

  /// <summary>
  /// The totp entry will only be enabled after the user verified they have a working totp setup.
  /// </summary>
  [JsonPropertyName("enabled")]
  public bool? Enabled { get; set; }

  [JsonPropertyName("secret")]
  public string Secret { get; set; } = "";

  /// <summary>
  /// The totp url used to be able to enroll the user later
  /// </summary>
  [JsonPropertyName("url")]
  public string Url { get; set; } = "";

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class TOTPPasscode
{

  [JsonPropertyName("passcode")]
  public string Passcode { get; set; } = "";

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class Token2
{

  [JsonPropertyName("created")]
  public DateTimeOffset? Created { get; set; }

  [JsonPropertyName("id")]
  public int Id { get; set; }

  [JsonPropertyName("token")]
  public string Token { get; set; } = "";

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class User
{

  /// <summary>
  /// A timestamp when this task was created. You cannot change this value.
  /// </summary>
  [JsonPropertyName("created")]
  public DateTimeOffset? Created { get; set; }

  /// <summary>
  /// The user's email address.
  /// </summary>
  [JsonPropertyName("email")]
  [System.ComponentModel.DataAnnotations.StringLength(250)]
  public string Email { get; set; } = "";

  /// <summary>
  /// The unique, numeric id of this user.
  /// </summary>
  [JsonPropertyName("id")]
  public int Id { get; set; }

  /// <summary>
  /// The full name of the user.
  /// </summary>
  [JsonPropertyName("name")]
  public string Name { get; set; } = "";

  /// <summary>
  /// A timestamp when this task was last updated. You cannot change this value.
  /// </summary>
  [JsonPropertyName("updated")]
  public DateTimeOffset? Updated { get; set; }

  /// <summary>
  /// The username of the user. Is always unique.
  /// </summary>
  [JsonPropertyName("username")]
  [System.ComponentModel.DataAnnotations.StringLength(250, MinimumLength = 1)]
  public string Username { get; set; } = "";

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class LinkShareAuth
{

  [JsonPropertyName("password")]
  public string Password { get; set; } = "";

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class UserAvatarProvider
{

  /// <summary>
  /// The avatar provider. Valid types are `gravatar` (uses the user email), `upload`, `initials`, `marble` (generates a random avatar for each user), `ldap` (synced from LDAP server), `openid` (synced from OpenID provider), `default`.
  /// </summary>
  [JsonPropertyName("avatar_provider")]
  public string AvatarProvider { get; set; } = "";

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class UserDeletionRequestConfirm
{

  [JsonPropertyName("token")]
  public string Token { get; set; } = "";

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class UserExportStatus
{

  [JsonPropertyName("created")]
  public DateTimeOffset? Created { get; set; }

  [JsonPropertyName("expires")]
  public string Expires { get; set; } = "";

  [JsonPropertyName("id")]
  public int Id { get; set; }

  [JsonPropertyName("size")]
  public int? Size { get; set; }

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class UserPassword
{

  [JsonPropertyName("new_password")]
  public string NewPassword { get; set; } = "";

  [JsonPropertyName("old_password")]
  public string OldPassword { get; set; } = "";

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class UserPasswordConfirmation
{

  [JsonPropertyName("password")]
  public string Password { get; set; } = "";

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class UserRegister
{

  /// <summary>
  /// The user's email address
  /// </summary>
  [JsonPropertyName("email")]
  [System.ComponentModel.DataAnnotations.StringLength(250)]
  public string Email { get; set; } = "";

  /// <summary>
  /// The language of the new user. Must be a valid IETF BCP 47 language code and exist in Vikunja.
  /// </summary>
  [JsonPropertyName("language")]
  public string Language { get; set; } = "";

  /// <summary>
  /// The user's password in clear text. Only used when registering the user. The maximum limi is 72 bytes, which may be less than 72 characters. This is due to the limit in the bcrypt hashing algorithm used to store passwords in Vikunja.
  /// </summary>
  [JsonPropertyName("password")]
  [System.ComponentModel.DataAnnotations.StringLength(72, MinimumLength = 8)]
  public string Password { get; set; } = "";

  /// <summary>
  /// The user's username. Cannot contain anything that looks like an url or whitespaces.
  /// </summary>
  [JsonPropertyName("username")]
  [System.ComponentModel.DataAnnotations.StringLength(250, MinimumLength = 3)]
  public string Username { get; set; } = "";

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class UserSettings
{

  /// <summary>
  /// If a task is created without a specified project this value should be used. Applies
  /// <br/>to tasks made directly in API and from clients.
  /// </summary>
  [JsonPropertyName("default_project_id")]
  public int? DefaultProjectId { get; set; }

  /// <summary>
  /// If true, the user can be found when searching for their exact email.
  /// </summary>
  [JsonPropertyName("discoverable_by_email")]
  public bool? DiscoverableByEmail { get; set; }

  /// <summary>
  /// If true, this user can be found by their name or parts of it when searching for it.
  /// </summary>
  [JsonPropertyName("discoverable_by_name")]
  public bool? DiscoverableByName { get; set; }

  /// <summary>
  /// If enabled, sends email reminders of tasks to the user.
  /// </summary>
  [JsonPropertyName("email_reminders_enabled")]
  public bool? EmailRemindersEnabled { get; set; }

  /// <summary>
  /// Additional settings links as provided by openid
  /// </summary>
  [JsonPropertyName("extra_settings_links")]
  public IDictionary<string, object> ExtraSettingsLinks { get; set; }

  /// <summary>
  /// Additional settings only used by the frontend
  /// </summary>
  [JsonPropertyName("frontend_settings")]
  public object FrontendSettings { get; set; }

  /// <summary>
  /// The user's language
  /// </summary>
  [JsonPropertyName("language")]
  public string Language { get; set; } = "";

  /// <summary>
  /// The new name of the current user.
  /// </summary>
  [JsonPropertyName("name")]
  public string Name { get; set; } = "";

  /// <summary>
  /// If enabled, the user will get an email for their overdue tasks each morning.
  /// </summary>
  [JsonPropertyName("overdue_tasks_reminders_enabled")]
  public bool? OverdueTasksRemindersEnabled { get; set; }

  /// <summary>
  /// The time when the daily summary of overdue tasks will be sent via email.
  /// </summary>
  [JsonPropertyName("overdue_tasks_reminders_time")]
  public string OverdueTasksRemindersTime { get; set; } = "";

  /// <summary>
  /// The user's time zone. Used to send task reminders in the time zone of the user.
  /// </summary>
  [JsonPropertyName("timezone")]
  public string Timezone { get; set; } = "";

  /// <summary>
  /// The day when the week starts for this user. 0 = sunday, 1 = monday, etc.
  /// </summary>
  [JsonPropertyName("week_start")]
  public int? WeekStart { get; set; }

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class UserWithSettings
{

  [JsonPropertyName("auth_provider")]
  public string AuthProvider { get; set; } = "";

  /// <summary>
  /// A timestamp when this task was created. You cannot change this value.
  /// </summary>
  [JsonPropertyName("created")]
  public DateTimeOffset? Created { get; set; }

  [JsonPropertyName("deletion_scheduled_at")]
  public string DeletionScheduledAt { get; set; } = "";

  /// <summary>
  /// The user's email address.
  /// </summary>
  [JsonPropertyName("email")]
  [System.ComponentModel.DataAnnotations.StringLength(250)]
  public string Email { get; set; } = "";

  /// <summary>
  /// The unique, numeric id of this user.
  /// </summary>
  [JsonPropertyName("id")]
  public int Id { get; set; }

  [JsonPropertyName("is_local_user")]
  public bool? IsLocalUser { get; set; }

  /// <summary>
  /// The full name of the user.
  /// </summary>
  [JsonPropertyName("name")]
  public string Name { get; set; } = "";

  [JsonPropertyName("settings")]
  public UserSettings Settings { get; set; }

  /// <summary>
  /// A timestamp when this task was last updated. You cannot change this value.
  /// </summary>
  [JsonPropertyName("updated")]
  public DateTimeOffset? Updated { get; set; }

  /// <summary>
  /// The username of the user. Is always unique.
  /// </summary>
  [JsonPropertyName("username")]
  [System.ComponentModel.DataAnnotations.StringLength(250, MinimumLength = 1)]
  public string Username { get; set; } = "";

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class AuthInfo
{

  [JsonPropertyName("ldap")]
  public LdapAuthInfo Ldap { get; set; }

  [JsonPropertyName("local")]
  public LocalAuthInfo Local { get; set; }

  [JsonPropertyName("openid_connect")]
  public OpenIDAuthInfo OpenidConnect { get; set; }

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class LdapAuthInfo
{

  [JsonPropertyName("enabled")]
  public bool? Enabled { get; set; }

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class LegalInfo
{

  [JsonPropertyName("imprint_url")]
  public string ImprintUrl { get; set; } = "";

  [JsonPropertyName("privacy_policy_url")]
  public string PrivacyPolicyUrl { get; set; } = "";

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class LocalAuthInfo
{

  [JsonPropertyName("enabled")]
  public bool? Enabled { get; set; }

  [JsonPropertyName("registration_enabled")]
  public bool? RegistrationEnabled { get; set; }

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class OpenIDAuthInfo
{

  [JsonPropertyName("enabled")]
  public bool? Enabled { get; set; }

  [JsonPropertyName("providers")]
  public ICollection<Provider> Providers { get; set; } = new List<Provider>();

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class VikunjaInfos
{

  [JsonPropertyName("auth")]
  public AuthInfo Auth { get; set; }

  [JsonPropertyName("available_migrators")]
  public ICollection<string> AvailableMigrators { get; set; } = new List<string>();

  [JsonPropertyName("caldav_enabled")]
  public bool? CaldavEnabled { get; set; }

  [JsonPropertyName("demo_mode_enabled")]
  public bool? DemoModeEnabled { get; set; }

  [JsonPropertyName("email_reminders_enabled")]
  public bool? EmailRemindersEnabled { get; set; }

  [JsonPropertyName("enabled_background_providers")]
  public ICollection<string> EnabledBackgroundProviders { get; set; } = new List<string>();

  [JsonPropertyName("frontend_url")]
  public string FrontendUrl { get; set; } = "";

  [JsonPropertyName("legal")]
  public LegalInfo Legal { get; set; }

  [JsonPropertyName("link_sharing_enabled")]
  public bool? LinkSharingEnabled { get; set; }

  [JsonPropertyName("max_file_size")]
  public string MaxFileSize { get; set; } = "";

  [JsonPropertyName("max_items_per_page")]
  public int? MaxItemsPerPage { get; set; }

  [JsonPropertyName("motd")]
  public string Motd { get; set; } = "";

  [JsonPropertyName("public_teams_enabled")]
  public bool? PublicTeamsEnabled { get; set; }

  [JsonPropertyName("task_attachments_enabled")]
  public bool? TaskAttachmentsEnabled { get; set; }

  [JsonPropertyName("task_comments_enabled")]
  public bool? TaskCommentsEnabled { get; set; }

  [JsonPropertyName("totp_enabled")]
  public bool? TotpEnabled { get; set; }

  [JsonPropertyName("user_deletion_enabled")]
  public bool? UserDeletionEnabled { get; set; }

  [JsonPropertyName("version")]
  public string Version { get; set; } = "";

  [JsonPropertyName("webhooks_enabled")]
  public bool? WebhooksEnabled { get; set; }

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class HTTPError
{

  [JsonPropertyName("code")]
  public int? Code { get; set; }

  [JsonPropertyName("message")]
  public string Message { get; set; } = "";

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public enum ProjectViewBucketConfigurationMode
{

  [System.Runtime.Serialization.EnumMember(Value = @"none")]
  None = 0,

  [System.Runtime.Serialization.EnumMember(Value = @"manual")]
  Manual = 1,

  [System.Runtime.Serialization.EnumMember(Value = @"filter")]
  Filter = 2,

}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public enum ProjectViewViewKind
{

  [System.Runtime.Serialization.EnumMember(Value = @"list")]
  List = 0,

  [System.Runtime.Serialization.EnumMember(Value = @"gantt")]
  Gantt = 1,

  [System.Runtime.Serialization.EnumMember(Value = @"table")]
  Table = 2,

  [System.Runtime.Serialization.EnumMember(Value = @"kanban")]
  Kanban = 3,

}

[System.CodeDom.Compiler.GeneratedCode("NSwag", "14.6.3.0 (NJsonSchema v11.5.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class FileResponse : System.IDisposable
{
  private System.IDisposable _client;
  private System.IDisposable _response;

  public int StatusCode { get; private set; }

  public IReadOnlyDictionary<string, IEnumerable<string>> Headers { get; private set; }

  public System.IO.Stream Stream { get; private set; }

  public bool IsPartial
  {
    get { return StatusCode == 206; }
  }

  public FileResponse(int statusCode, IReadOnlyDictionary<string, IEnumerable<string>> headers, System.IO.Stream stream, System.IDisposable client, System.IDisposable response)
  {
    StatusCode = statusCode;
    Headers = headers;
    Stream = stream;
    _client = client;
    _response = response;
  }

  public void Dispose()
  {
    Stream.Dispose();
    if (_response != null)
      _response.Dispose();
    if (_client != null)
      _client.Dispose();
  }
}
#endregion
