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
    private List<string> _proxies;
    private string _singleProxy;
    private string _testUrl;
    
    private bool _testedProxies = false;
    private Random _random = new Random();
    private int _currentProxyIndex = 0;
    private Stopwatch _stopwatch = Stopwatch.StartNew();
    private string _currentProxyUri;
    
    public ProxyManagerService(string testUrl, string proxyFile, string singleProxy, string proxyMode)
    {
        _testUrl = testUrl;
        _proxyFile = proxyFile;

        ProxyModeType proxyModeType = ProxyModeType.Random;
        Enum.TryParse<ProxyModeType>(proxyMode, out proxyModeType);
        this.ProxyMode = proxyModeType;
        
        _proxies = new List<string>();
        _singleProxy = singleProxy;
    }

    public async Task SetProxySettingsAsync(RestClientOptions options)
    {
        if (string.IsNullOrWhiteSpace(_proxyFile) && string.IsNullOrWhiteSpace(_singleProxy))
        {
            return;
        }
        
        ProxyModel? proxy = await GetProxyAsync();
        options.Proxy = proxy?.Proxy;
        if (options.Proxy != null)
        {
            options.Proxy.Credentials = proxy?.Credentials;
        }

        if (!string.Equals(_currentProxyUri, proxy?.ProxyUri))
        {
            Console.WriteLine($"Switched to proxy {proxy?.ProxyUri}");
        }

        if (proxy != null && string.IsNullOrWhiteSpace(_currentProxyUri))
        {
            Console.WriteLine($"Switched to proxy {proxy.ProxyUri}");
        }
        _currentProxyUri = proxy?.ProxyUri;
    }

    public void PickNextProxy()
    {
        _currentProxyIndex++;
        if (_currentProxyIndex >= _proxies.Count)
        {
            _currentProxyIndex = 0;
        }
    }
    
    public async Task<ProxyModel> GetProxyAsync()
    {
        if (!_testedProxies || _proxies.Count == 0)
        {
            await LoadProxyFileAsync();
        }
        
        string? proxy = string.Empty;

        switch (ProxyMode)
        {
            case ProxyModeType.Random:
                proxy = _proxies
                    .Skip(_random.Next(0, _proxies.Count))
                    .FirstOrDefault();
                break;
            case ProxyModeType.RoundRobin:
                proxy = _proxies
                    .Skip(_currentProxyIndex)
                    .FirstOrDefault();
                _currentProxyIndex++;
                break;
            case ProxyModeType.StickyTillError:
            case ProxyModeType.PerArtist:
                proxy = _proxies
                    .Skip(_currentProxyIndex)
                    .FirstOrDefault();
                break;
            case ProxyModeType.RotateTime:
                if (_stopwatch.Elapsed.Minutes >= 5)
                {
                    PickNextProxy();
                    _stopwatch.Restart();
                }
                
                proxy = _proxies
                    .Skip(_currentProxyIndex)
                    .FirstOrDefault();
                break;
        }

        if (_currentProxyIndex >= _proxies.Count)
        {
            _currentProxyIndex = 0;
        }
        
        if (string.IsNullOrWhiteSpace(proxy))
        {
            return null;
        }
        return GetProxy(proxy);
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
        List<string> testProxies = new List<string>();
        if (!string.IsNullOrWhiteSpace(_proxyFile))
        {
            FileInfo fileInfo = new FileInfo(_proxyFile);
            if (!fileInfo.Exists)
            {
                throw new FileNotFoundException("Proxies file not found", fileInfo.FullName);
            }
            
            testProxies = File
                .ReadAllLines(_proxyFile)
                .ToList();
            return;
        }

        if (!string.IsNullOrWhiteSpace(_singleProxy))
        {
            testProxies.Add(_singleProxy);
        }

        AsyncLock asyncLock = new AsyncLock();
        
        Stopwatch stopwatch = Stopwatch.StartNew();
        int testedProxies = 0;

        await ParallelHelper.ForEachAsync(testProxies, 100, async proxy =>
        {
            if (stopwatch.Elapsed.Seconds >= 5)
            {
                Console.WriteLine($"Tested proxies: {testedProxies} of {testProxies.Count}, working proxies: {_proxies.Count}");
                stopwatch.Restart();
            }
            
            if (await TestProxyAsync(proxy))
            {
                using (await asyncLock.LockAsync())
                {
                    _proxies.Add(proxy);
                }
            }
            testedProxies++;
        });
        
        
        Console.WriteLine($"Proxies working: {_proxies.Count}");

        if (testProxies.Count > 0 && _proxies.Count == 0)
        {
            throw new Exception("None of the proxies are working");
        }

        _testedProxies = true;
    }

    private async Task<bool> TestProxyAsync(string proxyUri)
    {
        ProxyModel proxyModel = GetProxy(proxyUri);

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
            
        }

        return false;
    }
}