# Memorial Server

<div  align=center>
    <img src="https://github.com/HIT-ReFreSH/HitGeneralServices/raw/master/images/Full_2048.png" width = 30% height = 30%  />
</div>

![DockerHub-v](https://img.shields.io/docker/v/ferdinandsu/memorial/latest?style=flat-square)
![DockerHub-DL](https://img.shields.io/docker/pulls/ferdinandsu/memorial?style=flat-square)
![DockerHub-size](https://img.shields.io/docker/image-size/ferdinandsu/memorial?style=flat-square)
![GitHub](https://img.shields.io/github/license/HIT-ReFreSH/Memorial?style=flat-square)
![GitHub last commit](https://img.shields.io/github/last-commit/HIT-ReFreSH/Memorial?style=flat-square)
![GitHub repo size](https://img.shields.io/github/repo-size/HIT-ReFreSH/Memorial?style=flat-square)
![GitHub code size](https://img.shields.io/github/languages/code-size/HIT-ReFreSH/Memorial?style=flat-square)

[View at DockerHub](https://hub.docker.com/repository/docker/ferdinandsu/memorial)

A simple http server provides Memorial Schedule service, can be easily deployed with docker.

## Usage

### Create config.json

In this format：

```json
{
  "Subscriptions": [
    {
      "Name": "MyMemorials",

      "Events": [
        {
          "Expression": "No. %NOC% Year of 21st Centry",
          "Since": "2020-01-12",
          "Repeat": 4,
          "Notification": [
            {
              "Expression": "%Offset% days To the New Year",
              "DayOffset": 1,
              "Time": "18:00:00"
            }
          ]
        }
      ],
      "TimeZone": "Asia/Shanghai",
      "Secret": "Jinitaimei"
    }
  ]
}
```

Obviously, you may add may subscriptions with different Name & Secret, where TimeZone is calendar's timezone, and several events contained. 
Each event has its first date Since, repeat type Repeat, and its expression ("%NOC%" means number of cycles, will be replaced by the value, starts from 0).
Notifications may be configured for each event.

Definitions of RepeatType and EventNotification are shown as following.

```csharp
public enum RepeatType
{
    None = 0,
    EveryDay = 1,
    EveryWeek = 2,
    EveryMonth = 3,
    EveryYear = 4,
}
```

```csharp
/// <summary>
/// 
/// </summary>
/// <param name="Expression">Expression of Notification</param>
/// <param name="DayOffset">The number of days the reminder needs to advance.</param>
/// <param name="Time">Time to show Notification</param>
public record EventNotification(string Expression, int DayOffset, TimeOnly Time);
```

Place the `config.json` to your server, remember to use UTF-8 encoding.

### Deployment

Please deploy with docker, use your own port number & config. **Remember to configure your firewall.**

```bash
docker pull ferdinandsu/memorial:latest
docker run -it --name memorial -v /root/config.json:/app/config.json -p 10086:80 -d docker.io/ferdinandsu/memorial:latest
```

### Use Subscription

GET the Subscription's Calendar with the following link：

```txt
http://ip:port/cal/<Name>?secret=<Secret>
```

GET the Subscription's today events with the following link：

```txt
http://ip:port/today/<Name>?secret=<Secret>
```