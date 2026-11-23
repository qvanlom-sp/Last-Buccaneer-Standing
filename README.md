# Last Buccaneer Standing

Last Buccaneer Standing is a single-player pirate game built with C# and standalone Blazor WebAssembly. It runs entirely in the browser. The map, ships, forts, and cannonballs use simple placeholder shapes.

## Run locally

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), then run the following from this folder. Open the local URL that the command prints.

```powershell
dotnet run --project Blackwake.csproj
```

## Play

1. Open the **Outfitter** and select a deck and cell on the ship grid. Fit a cannon and assign a gunner, or install a barricade. Row and deck set the firing lane. Place barricades in outboard columns to protect equipment behind them. Barricades stop opposing rounds until destroyed, and friendly shots pass through.
2. Shop and hire at a basecamp. Every basecamp stocks cannons and barricades. Captured forts offer different ships and stronger recruits as you explore farther. A pirate card shows **Swiftness** (reload time), **Accuracy** (shot spread), and their average. Each crewmate mans one gun. Refit owned gear or move your crew between guns while at sea.
3. Switch owned ships at a basecamp. Larger ships have more space and hull but steer and sail more slowly. Hull and rigging upgrades add percentages to the active ship's base stats. Fitted gear carries to cells that exist on the new ship, and anything that no longer fits goes into reserve.
4. On the **Sea chart**, click an outlined neighboring tile to sail and reveal the surrounding waters. The starting sloop gets seven moves per day. The chart stays fixed and each new campaign shuffles encounters among sites in their region.
5. Select **Engage in battle** or **Raid merchant** at a hostile location. Manned cannons fire automatically. Use **W/S**, **↑/↓**, the steering controls, or a click or tap on the battlefield to line shots up with targets and gates. **×2** doubles shot damage, **+1** splits off another ball, **F** ignites shots, and **×** blocks cannonballs. Destroyed cannons stop firing for the rest of the battle.
6. Every encounter has its own deck layout and crew. Merchants carry valuable treasure chests with light escorts aboard. Island batteries use their deck grid to layer defenses and protect land cannons. Pirate crews range from aggressive broadsides to more protected layouts. Farther regions field larger grids, better gunners, and more protected equipment.
7. Break a merchant's marked chests for doubloons, capture island forts to set new basecamps, and defeat all three regional overlords to rule the ocean. Retreat or defeat costs 20% of your pre-battle doubloons, rounded up, and returns you to your latest basecamp. Upgrades, unlocked territory, and merchant loot already recovered carry over. The ship starts each battle fully repaired.

Campaign progress saves in the current browser at the same site address. Reloading during a battle takes you back to the encounter's chart tile. Existing campaigns keep their map, inventory, hull upgrades, and steering upgrades. The old powder and gun crew upgrades are refunded in doubloons.

## Create the repository with GitHub Desktop

1. Open GitHub Desktop and sign in. Select **File → Add local repository** and choose the **Last Buccaneer Standing** folder containing `Blackwake.csproj`. If GitHub Desktop says the folder is not a Git repository, select **Create a repository** in that dialog and verify the displayed destination points to the existing game folder.
2. Review **Changes** and commit any game files that are still uncommitted.
3. If the repository has yet to be published, select **Publish repository**. For GitHub Pages on GitHub Free, clear **Keep this code private** only if you want the source published publicly. Select **Publish Repository**, then **Repository → View on GitHub**. [GitHub Desktop guide](https://docs.github.com/en/desktop/overview/creating-your-first-repository-using-github-desktop)

## Deploy to GitHub Pages

1. In the GitHub repository, open **Settings → Pages**. Under **Build and deployment**, set **Source** to **GitHub Actions**. The required workflow is already in `.github/workflows/pages.yml`. [GitHub Pages publishing source](https://docs.github.com/en/pages/getting-started-with-github-pages/configuring-a-publishing-source-for-your-github-pages-site)
2. Open **Actions → Deploy Last Buccaneer Standing to GitHub Pages → Run workflow**, choose the default branch, and run it. This starts a fresh run after Pages is configured.
3. Once the deploy job succeeds, open its **github-pages** link. Push future commits to the default branch from GitHub Desktop to redeploy automatically.

The workflow publishes only the Blazor static files. Relative URLs support hosting under the repository path. The build uses public .NET packages and needs no API keys or private package feed. [Microsoft's Blazor GitHub Pages guide](https://learn.microsoft.com/en-us/aspnet/core/blazor/host-and-deploy/webassembly/github-pages?view=aspnetcore-10.0)
