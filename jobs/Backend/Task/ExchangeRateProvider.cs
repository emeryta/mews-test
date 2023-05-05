using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System;

namespace ExchangeRateUpdater
{
    public class CNBResponse 
    {
        public List<CNBRate> Rates;        
    }

    public class CNBRate
    {
        public string CurrencyCode;
        public decimal Rate;

        public CNBRate(string currencyCode, decimal rate)
        {
            CurrencyCode = currencyCode;
            Rate = rate;
        }
    }

    public class ExchangeRateProvider
    {
        public const string currencyTargetCode = "CZK";        

        /// <summary>
        /// Should return exchange rates among the specified currencies that are defined by the source. But only those defined
        /// by the source, do not return calculated exchange rates. E.g. if the source contains "CZK/USD" but not "USD/CZK",
        /// do not return exchange rate "USD/CZK" with value calculated as 1 / "CZK/USD". If the source does not provide
        /// some of the currencies, ignore them.
        /// </summary>
        public IEnumerable<ExchangeRate> GetExchangeRates(IEnumerable<Currency> currencies)
        {
            var CNBRates = getRatesFromCNB();
            var rates = new List<ExchangeRate>();
            
            currencies.ToList().ForEach(x =>
            {
                if(x.Code == currencyTargetCode)
                {
                    rates.Add(new ExchangeRate(x, new Currency(currencyTargetCode), 1));
                }
                else
                {
                    if(CNBRates.Any(r => r.CurrencyCode == x.Code))
                    {
                        rates.Add(new ExchangeRate(x, new Currency(currencyTargetCode), CNBRates.Where(r => r.CurrencyCode == x.Code).Select(r => r.Rate).First()));
                    }
                }
            });

            return rates;
        }
        
        private List<CNBRate> getRatesFromCNB()
        {
            var url = $"https://api.cnb.cz/cnbapi/exrates/daily";
            var parameters = $"?date={ DateTime.Now.ToString("yyyy-MM-dd")}&lang=EN";

            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(url);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = client.GetAsync(parameters).Result;  
        
            if (response.IsSuccessStatusCode)
            {
                var jsonString = response.Content.ReadAsStringAsync().Result;
                var CNBResponse = JsonConvert.DeserializeObject<CNBResponse>(jsonString);

                return CNBResponse.Rates;
            }

            return new List<CNBRate>();
        }
    }
}
