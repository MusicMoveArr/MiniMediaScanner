using System.Diagnostics;
using System.Net;
using MiniMediaScanner.Enums;
using MiniMediaScanner.Helpers;
using MiniMediaScanner.Models;
using RestSharp;

namespace MiniMediaScanner.Services;

public class ProxyManagerService
{
    public ProxyModeType ProxyMode { get; set; }
    private readonly string _proxyFile;
    public readonly List<ProxyModel> WebProxies;
    private string _singleProxy;
    private string _testUrl;
    
    private bool _testedProxies = false;
    private Random _random = new Random();
    private int _currentProxyIndex = 0;
    private Stopwatch _stopwatch = Stopwatch.StartNew();
    private string _currentProxyUri;
    
    public int WorkingProxies => WebProxies?.Count ?? 0;
    
    public ProxyManagerService(string testUrl, string proxyFile, string singleProxy, string proxyMode)
    {
        _testUrl = testUrl;
        _proxyFile = proxyFile;

        ProxyModeType proxyModeType = ProxyModeType.Random;
        Enum.TryParse<ProxyModeType>(proxyMode, out proxyModeType);
        this.ProxyMode = proxyModeType;
        
        WebProxies = new List<ProxyModel>();
        _singleProxy = singleProxy;
    }

    public async Task SetProxySettingsAsync(RestClientOptions options)
    {
        if (string.IsNullOrWhiteSpace(_proxyFile) && 
            string.IsNullOrWhiteSpace(_singleProxy))
        {
            return;
        }
        
        ProxyModel? proxy = await GetProxyAsync();
        options.Proxy = proxy?.Proxy;
        if (options.Proxy != null && 
            proxy != null)
        {
            options.Proxy.Credentials = proxy?.Credentials;
            proxy.LastUsage = DateTime.Now;
            proxy.RequestCount++;
        }

        if (!string.Equals(_currentProxyUri, proxy?.ProxyUri))
        {
            //Debug.WriteLine($"Switched to proxy {proxy?.ProxyUri}");
        }

        if (proxy != null && string.IsNullOrWhiteSpace(_currentProxyUri))
        {
            //Debug.WriteLine($"Switched to proxy {proxy.ProxyUri}");
        }
        _currentProxyUri = proxy?.ProxyUri;
    }

    public void PickNextProxy()
    {
        _currentProxyIndex++;
        if (_currentProxyIndex >= WebProxies.Count)
        {
            _currentProxyIndex = 0;
        }
    }
    
    public async Task<ProxyModel?> GetProxyAsync()
    {
        if (!_testedProxies || WebProxies.Count == 0)
        {
            await LoadProxyFileAsync();
        }
        
        ProxyModel? proxy = null;

        switch (ProxyMode)
        {
            case ProxyModeType.Random:
                proxy = WebProxies
                    .Skip(_random.Next(0, WebProxies.Count))
                    .FirstOrDefault();
                break;
            case ProxyModeType.RoundRobin:
                proxy = WebProxies
                    .Skip(_currentProxyIndex)
                    .FirstOrDefault();
                _currentProxyIndex++;
                break;
            case ProxyModeType.StickyTillError:
            case ProxyModeType.PerArtist:
                proxy = WebProxies
                    .Skip(_currentProxyIndex)
                    .FirstOrDefault();
                break;
            case ProxyModeType.RotateTime:
                if (_stopwatch.Elapsed.Minutes >= 5)
                {
                    PickNextProxy();
                    _stopwatch.Restart();
                }
                
                proxy = WebProxies
                    .Skip(_currentProxyIndex)
                    .FirstOrDefault();
                break;
        }

        if (_currentProxyIndex >= WebProxies.Count)
        {
            _currentProxyIndex = 0;
        }
        
        return proxy;
    }

    private ProxyModel GetProxy(string proxy)
    {
        if (proxy.StartsWith("http"))
        {
            string[] splitUri = proxy.Split('@');
            string proxyUri = splitUri.FirstOrDefault();
            string username = string.Empty;
            string password = string.Empty;

            string auth = splitUri.Skip(1).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(auth))
            {
                string[] userPass = auth.Split(':');
                username = userPass.FirstOrDefault();
                password = userPass.Skip(1).FirstOrDefault();
            }

            return new ProxyModel(proxyUri, username, password);
        }

        return null;
    }

    private async Task LoadProxyFileAsync()
    {
        List<ProxyModel> testProxies = new List<ProxyModel>();

        if (string.IsNullOrWhiteSpace(_proxyFile))
        {
            return;
        }
        
        if (!string.IsNullOrWhiteSpace(_proxyFile))
        {
            FileInfo fileInfo = new FileInfo(_proxyFile);
            if (!fileInfo.Exists)
            {
                throw new FileNotFoundException("Proxies file not found", fileInfo.FullName);
            }
            
            testProxies = File
                .ReadAllLines(_proxyFile)
                .Select(proxy => GetProxy(proxy))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(_singleProxy))
        {
            testProxies.Add(GetProxy(_singleProxy));
        }

        AsyncLock asyncLock = new AsyncLock();
        
        Stopwatch stopwatch = Stopwatch.StartNew();
        int testedProxies = 0;

        await ParallelHelper.ForEachAsync(testProxies, 100, async proxy =>
        {
            if (stopwatch.Elapsed.Seconds >= 5)
            {
                Debug.WriteLine($"Tested proxies: {testedProxies} of {testProxies.Count}, working proxies: {WebProxies.Count}");
                stopwatch.Restart();
            }
            
            if (await TestProxyAsync(proxy))
            {
                using (await asyncLock.LockAsync())
                {
                    WebProxies.Add(proxy);
                }
            }
            testedProxies++;
        });
        
        Console.WriteLine($"Proxies working: {WebProxies.Count}");

        if (testProxies.Count > 0 && WebProxies.Count == 0)
        {
            throw new Exception("None of the proxies are working");
        }

        _testedProxies = true;
    }

    private async Task<bool> TestProxyAsync(ProxyModel proxyModel)
    {
        try
        {
            
            RestClientOptions options = new RestClientOptions(_testUrl);
            options.Proxy = proxyModel?.Proxy;
            options.Proxy.Credentials = proxyModel?.Credentials;
            options.Timeout = new TimeSpan(0, 0, 15);
            
            using RestClient client = new RestClient(options);
            RestRequest request = new RestRequest();
            var response = await client.GetAsync(request);
            
            return response?.StatusCode == HttpStatusCode.OK && response?.Content?.Length > 500;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Proxy error: {proxyModel.ProxyUri}, {e.Message}");
        }

        return false;
    }
}