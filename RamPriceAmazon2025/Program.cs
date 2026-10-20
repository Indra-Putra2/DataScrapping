using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

List<RamData> RamDataList = new();

var csvPath = Path.Combine(Directory.GetCurrentDirectory(), "RamData.csv");

var sb = new StringBuilder();
sb.AppendLine("Product Title,Variant,Price,ASIN");

var options = new ChromeOptions();
options.AddArgument("--start-maximized");
options.AddArgument("--disable-blink-features=AutomationControlled");
options.AddArgument(@"--user-data-dir=C:\selenium-profile");

var driver = new ChromeDriver(options);

driver.Navigate().GoToUrl("https://pcpartpicker.com/products/memory/");

var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));

List<ReadOnlyCollection<IWebElement>> CollectionOfRows = new List<ReadOnlyCollection<IWebElement>>();

IEnumerable<RamData> ScrapeRamData(IWebDriver driver, WebDriverWait wait)
{
    var maxPageEl = wait.Until(ExpectedConditions.VisibilityOfAllElementsLocatedBy(By.CssSelector(".pagination li")));
    var maxPage = 0;
    foreach (var page in maxPageEl)
    {
        string current = page.Text.Trim();
        // This tries to parse. If it fails (returns false), currentInt stays 0.
        if (!int.TryParse(current, out int currentInt)) currentInt = 0;
        if (currentInt > maxPage)
        {
            maxPage = currentInt;
        }
    }

    Console.WriteLine(maxPage);

    for (int page = 1; page <= maxPage; page++)
    {
        driver.Navigate().GoToUrl($"https://pcpartpicker.com/products/memory/#page={page}");
        driver.Navigate().Refresh();

        // 1. Wait for Javascript to report page complete
        wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));

        // 2. Wait for the specific rows to be visible (not just present)
        var rows = wait.Until(ExpectedConditions.VisibilityOfAllElementsLocatedBy(By.CssSelector("tr.tr__product")));

        foreach (var row in rows)
        {
            yield return ParseRow(row);
        }

        Console.WriteLine($"Scraped page {page}");
    }
}

RamData ParseRow(IWebElement currentData)
{
    var cells = currentData.FindElements(By.TagName("td"));

    var name = cells[1].Text.Trim();
    var speed = cells[2].Text.Trim();
    var module = cells[3].Text.Trim();
    var pricePerGb = cells[4].Text.Trim();
    var color = cells[5].Text.Trim();
    var fwl = cells[6].Text.Trim();
    var caslat = float.Parse(cells[7].Text.Trim());

    float rating = 0f;
    foreach (var star in cells[8].FindElements(By.CssSelector("svg")))
    {
        var cls = star.GetAttribute("class");
        if (cls.Contains("shape-star-full")) rating += 1f;
        else if (cls.Contains("shape-star-half")) rating += 0.5f;
    }

    var match = Regex.Match(cells[8].Text, @"\d+");
    int reviewCount = match.Success ? int.Parse(match.Value) : 0;

    match = Regex.Match(cells[9].Text, @"\$\d+\.\d+");
    string price = match.Success ? match.Value : "0";

    return new RamData(
        name, speed, module, fwl, caslat,
        pricePerGb, color, rating, reviewCount, price
    );
}

foreach (var ram in ScrapeRamData(driver, wait))
{
    RamDataList.Add(ram);
}

var csv = new StringBuilder(); 
csv.AppendLine("Name,Speed,Module,First Word Latency,CAS Latency,Price Per GB,Color,Rating,Review Count,Price"); 
foreach (var item in RamDataList) 
{ 
    csv.AppendLine($"{item.Name},{item.Speed},{item.Module},{item.FirstWordLatency},{item.CASLatency},{item.PricePerGB},{item.Color},{item.Rating},{item.ReviewCount},{item.Price}"); 
}
File.WriteAllText(csvPath, csv.ToString()); 

Console.WriteLine($"CSV saved to {csvPath}");
Console.WriteLine("Press ENTER to quit");
Console.ReadLine();

driver.Quit();
Environment.Exit(0);

public struct RamData
{
    public string Name;
    public string Speed;
    public string Module;
    public string FirstWordLatency;
    public float CASLatency;
    public string PricePerGB;
    public string Color;
    public float Rating;
    public int ReviewCount;
    public string Price;

    public RamData(
        string name,
        string speed,
        string module,
        string firstWordLatency,
        float casLatency,
        string pricePerGB = "N/A",
        string color = "N/A",
        float rating = 0,
        int reviewCount = 0,
        string price = "0")
    {
        this.Name = name;
        this.Speed = speed;
        this.Module = module;
        this.FirstWordLatency = firstWordLatency;
        this.CASLatency = casLatency;
        this.PricePerGB = pricePerGB;
        this.Color = color;
        this.Rating = rating;
        this.ReviewCount = reviewCount;
        this.Price = price;
    }
};