### oeclot Config 模板说明 (未使用 consul 配置)

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
			//下游接收到请求后 跳转的uri
			"DownstreamPathTemplate": "/todos/{id}",
			"DownstreamScheme": "https",
			"DownstreamHostAndPorts": [
				{
					"Host": "jsonplaceholder.typicode.com",
					"Port": 443
				}
			],
			//ocelot 接受到的请求地址
			"UpstreamPathTemplate": "/todos/{id}",
			"UpstreamHttpMethod": ["Get", "Post"]
		}
	],
	"GlobalConfiguration": {
		// 外部暴露的Url,当前能访问到ocelot的域名
		"BaseUrl": "https://ocelot.odinsam.com"
	}
}
```
