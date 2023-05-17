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

		bool waitAckAbortTrial = false;
		int abortCode = -1;

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

		
		void send_abort_code(int abortCode){
			var data = new Command();
			data.CMD = "ABORT_TRIAL";
			data.DATA_TYPE = "ABORT_CODE";
			data.DATA = abortCode + "";
			var datas = JsonUtility.ToJson(data);
			server.SendResponse(datas);
		}

		IEnumerator handleMsg(string msg_rcvd){
			string msg = msg_rcvd;
			// Debug.Log("cmd rcvd: " +  msg);
			Command cmd = JsonUtility.FromJson<Command>(msg);
			yield return 0;
			// Debug.Log("handleMsg: " + waitAckAbortTrial);
			if(waitAckAbortTrial){
				if(cmd.CMD.Equals("ACK_ABORT_TRIAL")){
					waitAckAbortTrial = false;
					server.SendResponse("success");
				}
				else{
					// Debug.Log("ignoring client msg, waiting for ACK_ABORT_TRIAL. Resending abort_code");
					this.send_abort_code(abortCode);
				}
			}else{
				if(cmd.CMD.Equals("ACK_ABORT_TRIAL")){
					waitAckAbortTrial = false;
					server.SendResponse("success");
				}
				else if(cmd.CMD.Equals("RESET")){
					var rd = new RESET_DATA();
					if(cmd.DATA_TYPE.Equals("RESET_DATA")){
						rd = JsonUtility.FromJson<RESET_DATA>(cmd.DATA);
					}
					yield return StartCoroutine(player.reset(rd.USE_SCREENSHOT, rd.SCREENSHOT_PATH));
					player.task.OnAbortTrial += (abortCode) => {
						waitAckAbortTrial = true;
						this.abortCode = abortCode;
						this.send_abort_code(abortCode);
					};
					var res = JsonUtility.ToJson("success");
					server.SendResponse(res);
				}
				else if(cmd.CMD.Equals("GET_ACTION_SIZE")){
					server.SendResponse("" + player.getActionSize());
				}
				else if(cmd.CMD.Equals("STEP")){
					yield return StartCoroutine(player.step());
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

}
