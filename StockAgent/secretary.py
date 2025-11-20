import json
import os
import openai
from log.custom_logger import log


def run_api(model, prompt, temperature: float = 0):
    openai.api_key = ""
    client = openai.OpenAI(api_key=openai.api_key)
    response = client.chat.completions.create(
        model=model,
        messages=[
            {"role": "user", "content": prompt},
        ],
        temperature=temperature,
    )
    resp = response.choices[0].message.content
    return resp


class Secretary:
    def __init__(self, model):
        self.model = model

    def get_response(self, prompt):
        return run_api(self.model, prompt)

    """
        用json形式返回结果，例如：
        {{{{"loan": "yes", "loan_type": 3, "amount": 1000}}}}
        如果不需贷款，则返回：
        {{{{"loan" : "no"}}}}
        :returns: loan_format_check, fail_response, loan
    """

    def check_loan(self, resp, max_loan) -> (bool, str, dict):
        # format check
        if isinstance(resp, str) and resp.count('{') == 1 and resp.count('}') == 1:
            start_idx = resp.index('{')
            end_idx = resp.index('}')
        else:
            log.logger.debug("Wrong json content in response: {}".format(resp))
            fail_response = "Wrong json format, there is no {} or more than one {} in response."
            return False, fail_response, None

        action_json = resp[start_idx: end_idx + 1]
        action_json = action_json.replace("\n", "").replace(" ", "")
        try:
            parsed_json = json.loads(action_json)
        except json.JSONDecodeError as e:
            print(e)
            log.logger.debug("Illegal json content in response: {}".format(resp))
            fail_response = "Illegal json format."
            return False, fail_response, None

        # content check
        try:
            if "loan" not in parsed_json:
                log.logger.debug("Wrong json content in response: {}".format(resp))
                fail_response = "Key 'loan' not in response."
                return False, fail_response, None

            if parsed_json["loan"].lower() not in ["yes", "no"]:
                log.logger.debug("Wrong json content in response: {}".format(resp))
                fail_response = "Value of key 'loan' should be yes or no."
                return False, fail_response, None

            if parsed_json["loan"].lower() == "no":
                if "loan_type" in parsed_json or "amount" in parsed_json:
                    log.logger.debug("Wrong json content in response: {}".format(resp))
                    fail_response = "Don't include loan_type or amount in response if value of key 'loan' is no."
                    return False, fail_response, None
                else:
                    return True, "", parsed_json

            if parsed_json["loan"].lower() == "yes":
                if "loan_type" not in parsed_json or "amount" not in parsed_json:
                    log.logger.debug("Wrong json content in response: {}".format(resp))
                    fail_response = "Should include loan_type and amount in response if value of key 'loan' is yes."
                    return False, fail_response, None
                if parsed_json["loan_type"] not in [0, 1, 2]:
                    log.logger.debug("Wrong json content in response: {}".format(resp))
                    fail_response = "Value of key 'loan_type' should be 0, 1 or 2."
                    return False, fail_response, None
                if parsed_json["amount"] <= 0 or parsed_json["amount"] > max_loan:
                    log.logger.debug("Wrong json content in response: {}".format(resp))
                    fail_response = f"Value of key 'amount' should be positive and less than {max_loan}"
                    return False, fail_response, None
                return True, "", parsed_json

            log.logger.error("UNSOLVED LOAN JSON RESPONSE:{}".format(parsed_json))
            return False, "", None
        except Exception as e:
            log.logger.error("UNSOLVED LOAN JSON RESPONSE:{}".format(parsed_json))
            return False, "", None

    def check_action(self, resp, cash, stock_a_amount,
                     stock_b_amount, stock_a_price, stock_b_price, is_crypto=False) -> (bool, str, dict):
        # format check
        if isinstance(resp, str) and resp.count('{') == 1 and resp.count('}') == 1:
            start_idx = resp.index('{')
            end_idx = resp.index('}')
        else:
            log.logger.debug("Wrong json content in response: {}".format(resp))
            fail_response = "Wrong json format, there is no {} or more than one {} in response."
            return False, fail_response, None

        action_json = resp[start_idx: end_idx + 1]
        action_json = action_json.replace("\n", "").replace(" ", "")
        try:
            parsed_json = json.loads(action_json)
        except json.JSONDecodeError as e:
            print(e)
            log.logger.debug("Illegal json content in response: {}".format(resp))
            fail_response = "Illegal json format."
            return False, fail_response, None

        # content check
        try:
            if is_crypto:
                # Crypto mode: use symbol instead of stock, amounts can be fractional
                import util
                valid_symbols = [util.CRYPTO_SYMBOL_1, util.CRYPTO_SYMBOL_2]
                prices = {util.CRYPTO_SYMBOL_1: stock_a_price, util.CRYPTO_SYMBOL_2: stock_b_price}
                holds = {util.CRYPTO_SYMBOL_1: stock_a_amount, util.CRYPTO_SYMBOL_2: stock_b_amount}
            else:
                # Stock mode: use A/B
                prices = {"A": stock_a_price, "B": stock_b_price}
                holds = {"A": stock_a_amount, "B": stock_b_amount}
            
            if "action_type" not in parsed_json:
                log.logger.debug("Wrong json content in response: {}".format(resp))
                fail_response = "Key 'action_type' not in response."
                return False, fail_response, None

            if parsed_json["action_type"].lower() not in ["buy", "sell", "no"]:
                log.logger.debug("Wrong json content in response: {}".format(resp))
                fail_response = "Value of key 'action_type' should be 'buy', 'sell' or 'no'."
                return False, fail_response, None

            if parsed_json["action_type"].lower() == "no":
                if is_crypto:
                    if "symbol" in parsed_json or "amount" in parsed_json:
                        log.logger.debug("Wrong json content in response: {}".format(resp))
                        fail_response = "Don't include symbol or amount in response if value of key 'action_type' is no."
                        return False, fail_response, None
                else:
                    if "stock" in parsed_json or "amount" in parsed_json:
                        log.logger.debug("Wrong json content in response: {}".format(resp))
                        fail_response = "Don't include stock or amount in response if value of key 'action_type' is no."
                        return False, fail_response, None
                return True, "", parsed_json
            else:
                # Check required fields
                if is_crypto:
                    if "symbol" not in parsed_json or "amount" not in parsed_json or "price" not in parsed_json:
                        log.logger.debug("Wrong json content in response: {}".format(resp))
                        fail_response = "Should include symbol, amount and price in response " \
                                        "if value of key 'action_type' is buy or sell."
                        return False, fail_response, None
                    if parsed_json["symbol"] not in valid_symbols:
                        log.logger.debug("Wrong json content in response: {}".format(resp))
                        fail_response = f"Value of key 'symbol' should be one of {valid_symbols}."
                        return False, fail_response, None
                    asset_key = parsed_json["symbol"]
                else:
                    if "stock" not in parsed_json or "amount" not in parsed_json or "price" not in parsed_json:
                        log.logger.debug("Wrong json content in response: {}".format(resp))
                        fail_response = "Should include stock, amount and price in response " \
                                        "if value of key 'action_type' is buy or sell."
                        return False, fail_response, None
                    if parsed_json["stock"] not in ['A', 'B']:
                        log.logger.debug("Wrong json content in response: {}".format(resp))
                        fail_response = "Value of key 'stock' should be 'A' or 'B'."
                        return False, fail_response, None
                    asset_key = parsed_json["stock"]
                
                if parsed_json["price"] <= 0:
                    log.logger.debug("Wrong json content in response: {}".format(resp))
                    fail_response = f"Value of key 'price' should be positive."
                    return False, fail_response, None
                
                # For crypto, amount can be fractional (float); for stock, must be integer
                if not is_crypto and not isinstance(parsed_json["amount"], int):
                    log.logger.debug("Wrong json content in response: {}".format(resp))
                    fail_response = f"Value of key 'amount' should be integer (stock mode)."
                    return False, fail_response, None
                
                if is_crypto and not isinstance(parsed_json["amount"], (int, float)):
                    log.logger.debug("Wrong json content in response: {}".format(resp))
                    fail_response = f"Value of key 'amount' should be a number (crypto mode)."
                    return False, fail_response, None

                # buy more than cash or sell more than hold amount
                price = parsed_json["price"]
                if parsed_json["action_type"].lower() == "buy":
                    # For crypto, include fee in cost calculation
                    if is_crypto:
                        total_cost = parsed_json["amount"] * price * (1 + util.CRYPTO_FEE_RATE)
                        if parsed_json["amount"] <= 0 or total_cost > cash:
                            log.logger.debug("Buy more than cash (crypto): {}".format(resp))
                            fail_response = f"The cash you have now is {cash}, " \
                                            f"the value of 'amount' * 'price' * (1 + fee) " \
                                            f"should be positive and not exceed cash."
                            return False, fail_response, None
                    else:
                        if parsed_json["amount"] <= 0 or parsed_json["amount"] * price > cash:
                            log.logger.debug("Buy more than cash: {}".format(resp))
                            fail_response = f"The cash you have now is {cash}, " \
                                            f"the value of 'amount' * 'price'  " \
                                            f"should be positive and not exceed cash."
                            return False, fail_response, None

                hold_amount = holds[asset_key]
                if parsed_json["action_type"].lower() == "sell":
                    if parsed_json["amount"] <= 0 or parsed_json["amount"] > hold_amount:
                        asset_name = "crypto" if is_crypto else "stock"
                        log.logger.debug(f"Sell more than hold ({asset_name}): {resp}")
                        fail_response = f"The amount of {asset_name} you hold is {hold_amount}, " \
                                        f"the value of 'amount' should be positive and not exceed the " \
                                        f"amount you hold."
                        return False, fail_response, None
                return True, "", parsed_json

        except Exception as e:
            log.logger.error("UNSOLVED ACTION JSON RESPONSE:{}".format(parsed_json))
            return False, "", None

    def check_estimate(self, resp, is_crypto=False):
        # format check
        if isinstance(resp, str) and resp.count('{') == 1 and resp.count('}') == 1:
            start_idx = resp.index('{')
            end_idx = resp.index('}')
        else:
            log.logger.debug("Wrong json content in response: {}".format(resp))
            fail_response = "Wrong json format, there is no {} or more than one {} in response."
            return False, fail_response, None

        action_json = resp[start_idx: end_idx + 1]
        action_json = action_json.replace("\n", "").replace(" ", "")
        try:
            parsed_json = json.loads(action_json)
        except json.JSONDecodeError as e:
            print(e)
            log.logger.debug("Illegal json content in response: {}".format(resp))
            fail_response = "Illegal json format."
            return False, fail_response, None

        # content check
        try:
            if is_crypto:
                import util
                crypto_symbol_1 = util.CRYPTO_SYMBOL_1
                crypto_symbol_2 = util.CRYPTO_SYMBOL_2
                required_keys = [f"buy_{crypto_symbol_1}", f"buy_{crypto_symbol_2}",
                                f"sell_{crypto_symbol_1}", f"sell_{crypto_symbol_2}", "loan"]
            else:
                required_keys = ["buy_A", "buy_B", "sell_A", "sell_B", "loan"]
            
            missing_keys = [key for key in required_keys if key not in parsed_json]
            if missing_keys:
                log.logger.debug("Wrong json content in response: {}".format(resp))
                fail_response = f"Keys {required_keys} should be in response. Missing: {missing_keys}"
                return False, fail_response, None

            for key, item in parsed_json.items():
                if item not in ['yes', 'no']:
                    log.logger.debug("Wrong json content in response: {}".format(resp))
                    fail_response = "Value of all keys should be 'yes' or 'no'."
                    return False, fail_response, None
            return True, "", parsed_json

        except Exception as e:
            log.logger.error("UNSOLVED ESTIMATE JSON RESPONSE:{}".format(parsed_json))
            return False, "", None
