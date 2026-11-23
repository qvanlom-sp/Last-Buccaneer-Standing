# Blackwake

Blackwake is a single-player pirate game built with C# and standalone Blazor WebAssembly. It runs entirely in the browser. The map, ships, forts, and cannonballs use simple placeholder shapes.

## Run locally

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), then run the following from this folder. Open the local URL that the command prints.

```powershell
dotnet run --project Blackwake.csproj
```

## Play

1. Open the **Outfitter** to buy cannons, assign them to one of five mounts, and upgrade your hull, powder, gun crew, or helmsman. The mounts control the height from which your shots launch.
2. On the **Sea chart**, click an outlined neighboring tile to sail and reveal the surrounding waters. You get seven moves per day. The chart stays fixed and each new campaign shuffles encounters among sites in their region.
3. Select **Engage in battle** or **Raid merchant** at a hostile location. Your cannons fire automatically. Use **W/S**, **↑/↓**, the steering controls, or a click or tap on the battlefield to line shots up with targets and gates.
4. **×2** doubles shot damage, **+1** splits off another ball, **F** ignites shots, and **×** blocks incoming or outgoing cannonballs. A destroyed cannon stops firing for the rest of the battle. Shots that miss active cannons and chests can damage the hull.
5. Break a merchant's marked chests for doubloons, capture island forts to set new basecamps, and defeat all three regional overlords to rule the ocean. Retreat or defeat costs 20% of your pre-battle doubloons, rounded up, and returns you to your latest basecamp. Upgrades, unlocked territory, and merchant loot already recovered carry over. The ship starts each battle fully repaired.

Campaign progress saves in the current browser. Reloading during a battle takes you back to the encounter's chart tile.

## Create the repository with GitHub Desktop

1. Open GitHub Desktop and sign in. Select **File → Add local repository** and choose the existing **Pirate Game** folder containing `Blackwake.csproj`. If GitHub Desktop says the folder is not a Git repository, select **Create a repository** in that dialog. In the creation screen, verify the displayed destination is your existing **Pirate Game** folder. If the destination is shown as parent plus name, use the **Programs** folder as the local path and **Pirate Game** as the name. The project already includes a README and a gitignore.
2. Review **Changes** and commit any game files that are still uncommitted. Use a summary such as `Add Blackwake prototype`.
3. Select **Publish repository**. Name the GitHub repository `blackwake` if you want the example URL below. For GitHub Pages on GitHub Free, clear **Keep this code private** only if you want the source published publicly. Select **Publish Repository**, then **Repository → View on GitHub**. [GitHub Desktop guide](https://docs.github.com/en/desktop/overview/creating-your-first-repository-using-github-desktop)

## Deploy to GitHub Pages

1. In the GitHub repository, open **Settings → Pages**. Under **Build and deployment**, set **Source** to **GitHub Actions**. The required workflow is already in `.github/workflows/pages.yml`. [GitHub Pages publishing source](https://docs.github.com/en/pages/getting-started-with-github-pages/configuring-a-publishing-source-for-your-github-pages-site)
2. Open **Actions → Deploy Blackwake to GitHub Pages → Run workflow**, choose the default branch, and run it. This starts a fresh run after Pages is configured.
3. Once the deploy job succeeds, open its **github-pages** link. A repository named `blackwake` normally appears at `https://YOUR-USERNAME.github.io/blackwake/`. Push future commits to the default branch from GitHub Desktop to redeploy automatically.

The workflow publishes only the Blazor static files. Relative URLs support hosting under the repository path. The build uses public .NET packages and needs no API keys or private package feed. [Microsoft's Blazor GitHub Pages guide](https://learn.microsoft.com/en-us/aspnet/core/blazor/host-and-deploy/webassembly/github-pages?view=aspnetcore-10.0)
