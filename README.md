# TelegramBotForCarInsuranceSales

This telegram bot uses mindee for extracting data from documents and Azure OpenAI for assisting.

Workflow:
- When user start conversetion, bot sends messege where he intraduses himself and asks for passport photo and waits for the photo
- If user asks soming while bot is weiting for a photo, it should just answer a question
- After user sent the photo of passport, mindee processes the photo and the bot sends the result of it to ask for confirmation
- If user confirmes the data it asks for vehicle identification document and waits, otherwise it ask to send a photo of passport again and waits for the photo
- After user sent the photo of vehicle, mindee processes the photo and the bot sends the result of it to ask for confirmation
- If user confirmes the data it asks for fixed price confirmation (100 usd)
- If user confirmes the price api generates policy document and sends to the user, otherwise it says that there is no other price and asks for confirmation again

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
