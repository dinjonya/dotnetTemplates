### oeclot Config 模板说明 (使用 consul 配置)

1. 修改项目 launchSettings.json 文件的"ASPNETCORE_ENVIRONMENT"节点 value 为对应要使用的文件的文件名

2. dev.json 或者 pro.json 文件中的配置为 ocelot 业务配置

---

## 配置文件详细说明

### 1. dev.json 配置文件详细说明(pro.json 相同):

    dev.json 与 pro.json 配置唯一的区别只在于开发环境的 ip 与生产环境的 ip 区别。

```json
{
	"Routes": [
		{
			/*
            Ocelot 允许您将查询字符串指定为 DownstreamPathTemplate 的一部分
            DownstreamPathTemplate 的查询字符串{unitId} 作为了 UpstreamPathTemplate 中的一部分出现

            "DownstreamPathTemplate": "/api/subscriptions/{subscriptionId}/updates?unitId={unitId}",
			"UpstreamPathTemplate": "/api/units/{subscriptionId}/{unitId}/updates",

            反之亦可 e.g.

            "DownstreamPathTemplate": "/api/units/{subscriptionId}/{unitId}/updates",
            "UpstreamPathTemplate": "/api/subscriptions/{subscriptionId}/updates?unitId={unitId}",
            DownstreamPathTemplate的{unitId}部分最为UpstreamPathTemplate的查询字符串
            */
			"DownstreamPathTemplate": "/todos/{id}",
			"DownstreamScheme": "https",
			"DownstreamHostAndPorts": [
				{
					"Host": "jsonplaceholder.typicode.com",
					"Port": 443
				}
			],
			"UpstreamPathTemplate": "/todos/{id}",
			"UpstreamHttpMethod": ["Get"],
			// 默认的 ReRouting 配置不区分大小写
			"RouteIsCaseSensitive": false,
			/*
             0 是最低优先级  当有多个可能会匹配到的上优势优先选择 优先级大的上游配置匹配
             e.g.--
             "UpstreamPathTemplate": "/goods/{catchAll}"
             "Priority": 0

             "UpstreamPathTemplate": "/goods/delete"
             "Priority": 1

            请求 /goods/delete时
            如果没有Priority，则选择 /goods/{catchAll}
            当前情况 选择 /goods/delete  因为他的优先级Priority大
            */
			"Priority": 0,
			// 当前route唯一标识
			"Key": "odinsam-1",
			/*
            限速
            Ocelot 支持上游请求的速率限制，以便您的下游服务不会过载
            */
			"RateLimitOptions": {
				// 客户端白名单的数组
				"ClientWhitelist": [],
				// 指定启用端点速率限制
				"EnableRateLimiting": false,
				// 指定限制适用的周期，例如 1s、5m、1h、1d 等.如果您在此期间发出的请求数超出了限制所允许的数量，则您需要等待 PeriodTimespan 过去，然后再发出另一个请求
				"Period": "1s",
				// 指定我们可以在特定 秒 数后重试
				"PeriodTimespan": 5,
				// 指定客户端在定义的时间段内可以发出的最大请求数
				"Limit": 1
			},
			/*
            缓存
            1. Install-Package Ocelot.Cache.CacheManager
            2. ConfigureServices添加代码如下:
                services.AddOcelot()
                        .AddCacheManager(x =>
                        {
                            x.WithDictionaryHandle();
                        })
            ttl 秒设置为 15，这意味着缓存将在 15 秒后过期
            Region是对缓存进行的一个分区，我们可以调用Ocelot的 administration API来移除某个区下面的缓存 
            */
			"FileCacheOptions": { "TtlSeconds": 15, "Region": "somename" },
			/*
            负载均衡
            LeastConnection - 跟踪哪些服务正在处理请求，并向现有请求最少的服务发送新请求
            RoundRobin - 循环访问可用服务并发送请求
            NoLoadBalancer - 从配置或服务发现中获取第一个可用服务
            CookieStickySessions - 使用 cookie 将所有请求粘贴到特定服务器
            */
			"LoadBalancerOptions": {
				"Type": "LeastConnection"
			},
			/*
            服务质量与熔断 - 停止将请求转发到下游服务  Ocelot 将默认为所有下游请求超时 90 秒
            Install-Package Ocelot.Provider.Polly
            services.AddOcelot().AddPolly();
            */
			"QoSOptions": {
				// ExceptionsAllowedBeforeBreaking 允许多少个异常请求
				"ExceptionsAllowedBeforeBreaking": 5,
				// DurationOfBreak 熔断的时间，单位为秒
				"DurationOfBreak": 5,
				// TimeoutValue 如果下游请求的处理时间超过多少则自如将请求设置为超时  可以只单独设置这一个属性
				"TimeoutValue": 5000
			}
		},
		{
			"DownstreamPathTemplate": "/saveApiRecord",
			"DownstreamScheme": "https",
			"DownstreamHostAndPorts": [
				{
					"Host": "jsonplaceholder.typicode.com",
					"Port": 443
				}
			],
			"UpstreamPathTemplate": "/saveApiRecord",
			"UpstreamHttpMethod": ["Get"],
			"RouteIsCaseSensitive": false,
			"Priority": 0,
			"Key": "odinsam-2"
		}
	],
	/*
    请求聚合：一个客户端向服务器发出多个请求的地方
    注意：
    1.不能在路由和聚合之间有重复的 UpstreamPathTemplates  
    odinsam-1 路由1 UpstreamPathTemplates /todos/{id}
    odinsam-2 路由1 UpstreamPathTemplates /saveApiRecord
    聚合 UpstreamPathTemplates  /agg
    2. 仅支持GET方式
    3. 下游服务返回类型要求为application/json
    4. 返回内容类型为application/json，不会返回404请求
    5. 所有标头都将从下游服务响应中丢失。
    6. 如果您的下游服务返回 404，则聚合将不会为该下游服务返回任何内容。即使所有下游都返回 404，它也不会将聚合响应更改为 404。
    net core代码：
    // 必须将 FakeDefinedAggregator 添加到 OcelotBuilder 中
    services.AddOcelot()
            .AddSingletonDefinedAggregator<FakeDefinedAggregator>();
            // 瞬态聚合器
            // .AddTransientDefinedAggregator<FakeDefinedAggregator>();


    制作一个聚合器，必须实现这个接口
    public interface IDefinedAggregator
    {
        Task<DownstreamResponse> Aggregate(List<HttpContext> responses);
    }
    自定义聚合器  
    // 将两个聚合请求的结果，用分号拼接起来返回
    public class FakeDefinedAggregator : IDefinedAggregator
    {
        public FakeDefinedAggregator()
        {
        }
        public async Task<DownstreamResponse> Aggregate(List<HttpContext> responses)
        {

            List<string> result = new List<string>();
            foreach (var item in responses)
            {
                byte[] tmp = new byte[item.Response.Body.Length];
                await item.Response.Body.ReadAsync(tmp, 0, tmp.Length);
                var val = Encoding.UTF8.GetString(tmp);
                result.Add(val);
            }
            var merge = string.Join(";", result.ToArray());
            List<Header> headers = new List<Header>();
            return new DownstreamResponse(new StringContent(merge), HttpStatusCode.OK, headers, "some reason");
        }
    }
    */
	"Aggregates": [
		{
			"RouteKeys": ["odinsam-1", "odinsam-2"],
			"UpstreamPathTemplate": "/agg",
			/*
            Aggregator 聚合处理器  没有这个节点 可以完成普通聚合
            Aggregator节点需要代码支撑，完成自定义高级聚合
            */
			"Aggregator": "FakeDefinedAggregator"
		}
	],
	"GlobalConfiguration": {
		// Consul 服务配置
		// Install-Package Ocelot.Provider.Consul
		// services.AddOcelot().AddConsul().AddConfigStoredInConsul();
		"ServiceDiscoveryProvider": {
			"Host": "127.0.0.1",
			"Port": 9500
			// 轮训consul服务器 获得 最新的服务
			// "Type": "PollConsul",
			// "PollingInterval": 100
		},
		// 限流全局配置
		"RateLimitOptions": {
			// 指定是否禁用 X-Rate-Limit 和 Retry-After 标头
			"DisableRateLimitHeaders": false,
			// 指定超出的消息
			"QuotaExceededMessage": "Customize Tips!",
			// 指定发生速率限制时返回的 HTTP 状态代码
			"HttpStatusCode": 999,
			// 允许您指定用于识别客户端的标头。默认情况下它是“ClientId”
			"ClientIdHeader": "Test"
		}
	}
}
```
