using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace WeiXin
{
    public class PaynotifyModel
    {

        /**
         * 
                  {
                    "appid": "wx93ff2335f40970e0",
                    "bank_type": "CFT",
                    "cash_fee": "1",
                    "fee_type": "CNY",
                    "is_subscribe": "Y",
                    "mch_id": "1281976101",
                    "nonce_str": "dc7f28e5e11541abb78888ef4bfed142",
                    "openid": "oTndywz4YzGGYQvty1LXVIYtgy9A",
                    "out_trade_no": "16051114351025",
                    "result_code": "SUCCESS",
                    "return_code": "SUCCESS",
                    "sign": "D6C4FF99545BDFF0BD119816D24F2317",
                    "time_end": "20160511143518",
                    "total_fee": "1",
                    "trade_type": "JSAPI",
                    "transaction_id": "4009042001201605115734141053"
                  }

         * 
         ***/



        /// <summary>
        /// 
        /// </summary>
        public string appid { get; set; }  
                            
        public string bank_type { get; set; }                                             
        public string cash_fee { get; set; }      
        public string fee_type { get; set; }           
        public string is_subscribe { get; set; }                                             
        public string mch_id { get; set; }                                         
        public string nonce_str { get; set; }                                             
        public string openid { get; set; }                                                          
        public string out_trade_no { get; set; }                                               	
        public string result_code { get; set; }                                                     
        public string return_code { get; set; }                                                
        public string sign { get; set; }                                                        
        public string time_end { get; set; }                                       
        public string total_fee { get; set; }     
        public string trade_type { get; set; }    
        public string transaction_id { get; set; }
    }                                             
                                                  
}                                                 
                                                  
                                                  
                                                  
                                                  
                                                  
                                                  
                                                  