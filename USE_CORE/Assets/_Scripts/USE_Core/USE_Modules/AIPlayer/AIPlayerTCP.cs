using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AIPlayer{

	public class Command{
		public string CMD = null;
		public string DATA_TYPE = null;
		public string DATA = null;
	}

	public class RESET_DATA{
		public bool USE_SCREENSHOT = false;
		public string SCREENSHOT_PATH = "screenshot.jpg";
	}

	public class AIPlayerTCP : MonoBehaviour {

		public AIPlayer player;
		public TCPServerAI server;

		string new_msg = null;
		// Use this for initialization
		void Start () {
			server.OnReceiveMsg += (msg) => {
				new_msg = msg;
			};
		}

		void Update(){
			if(new_msg != null){
				StartCoroutine(handleMsg(new_msg));	
				new_msg = null;
			}
		}

		

		IEnumerator handleMsg(string msg_rcvd){
			string msg = msg_rcvd;
			Command cmd = JsonUtility.FromJson<Command>(msg);
			yield return 0;
			if(cmd.CMD.Equals("RESET")){
				var rd = new RESET_DATA();
				if(cmd.DATA_TYPE.Equals("RESET_DATA")){
					rd = JsonUtility.FromJson<RESET_DATA>(cmd.DATA);
				}
				yield return StartCoroutine(player.reset(rd.USE_SCREENSHOT, rd.SCREENSHOT_PATH));
				player.task.OnAbortTrial += (abortCode) => {
					var data = new Command();
					data.CMD = "ABORT_TRIAL";
					data.DATA_TYPE = "ABORT_CODE";
					data.DATA = abortCode + "";
					var datas = JsonUtility.ToJson(data);
					server.SendResponse(datas);
				};
				var res = JsonUtility.ToJson("success");
				server.SendResponse(res);
			}
			else if(cmd.CMD.Equals("GET_ACTION_SIZE")){
				server.SendResponse("" + player.getActionSize());
			}
			else if(cmd.CMD.Equals("NEXT")){
				yield return StartCoroutine(player.next());
				var observation = player.observation;
				var res = JsonUtility.ToJson(observation);
				server.SendResponse(res);
			}
			else if(cmd.CMD.Equals("ACT")){
				
				int action = 0;
				if(cmd.DATA_TYPE.Equals("INTEGER_ACTION")){
					action = int.Parse(cmd.DATA);
				}

				yield return StartCoroutine(player.act(action));
				var stepResult = player.stepResult;

				var res = JsonUtility.ToJson(stepResult);
				server.SendResponse(res);

			}
		}
	}

}
