import random
from time import sleep
from use_client import USE_Client

class Agent:
    def __init__(self):
        self.aborted = False
        self.use = USE_Client(debug=False)
        self.use.reset(callbackAbortTrial=self.callbackAbort, use_screenshot=True)
        self.action_size = self.use.get_action_size()

    def callbackAbort(self, abortCode):
        print("aborted:", abortCode)
        self.aborted = True

    def decide_action(self, obs):
        # i = random.randint(4, 6) # to test abort trial behavior
        i = 0 # to test min time required for a trial
        while i > 0:
            i = i - 1
            print('remaining time to decide:' + str(i))
            sleep(1)
            if self.aborted:
                print('wait, what? the trial is aborted..! I have to be quicker next time.')
                self.aborted = False
                self.use.ack_abort_trial()
                return None
        action = random.randint(0, self.action_size - 1)
        return action

    def play(self):
        while True:
            print("")
            obs = self.use.step()
            print("obs:", obs)
            action = self.decide_action(obs)
            if action is not None:
                response = self.use.act(action)
                print("action response:", response)
                if response is None:
                    continue
                reward = response['reward']
                isTrialEnd = response['isTrialEnd']
                isBlockEnd = response['isBlockEnd']
                isExperimentEnd = response['isExperimentEnd']
                # print("reward:", reward, "isBlockEnd:", isBlockEnd, "isExperimentEnd:", isExperimentEnd)
                if isExperimentEnd:
                    break


agent = Agent()
agent.play()