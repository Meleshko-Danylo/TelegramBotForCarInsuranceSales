# TelegramBotForCarInsuranceSales

This telegram bot uses mindee for extracting data from documents and Azure OpenAI for assisting

To run this project you need to have instoled next dependences:
- .NET 8.0
- Azure.AI.OpenAI (v2.1.0)
- Microsoft.AspNetCore.OpenApi (v8.0.0)
- Mindee (v3.26.0)
- Telegram.Bot (v22.5.1-dev.1)

You also have to go to appseting.json file and change some values:
- TelegramBot:Token - token for your new bot that you have to get from BotFather in telegram
- OpenAI values: this values you should get from azure when you create openIA servise
    - ApiKey: This is your unique authentication key for accessing OpenAI's API
    - Endpoint: The base URL where API requests should be sent (For direct OpenAI API access, this is typically https://api.openai.com/v1), If using Azure OpenAI, this would be your Azure endpoint URL
    - DeploymentName: Specifies which AI model to use for requests
- Mindee:ApiKey - authentication key to access Mindee's API for document parsing