import copy
import unittest

from parity import compare_outputs, numeric_inputs


def output(values, path=None):
    return [{"name": "Value", "branches": [{"path": [0] if path is None else path, "values": values}]}]


def number(value):
    return {"kind": "number", "value": str(value)}


class ParityComparisonTests(unittest.TestCase):
    def test_compares_values_beyond_mcp_sample_limit(self):
        first = output([number(value) for value in range(10)])
        second = copy.deepcopy(first)
        second[0]["branches"][0]["values"][9] = number(99)
        self.assertIn("item 9", compare_outputs(first, second)[0])

    def test_exact_paths_and_empty_branches_are_significant(self):
        self.assertTrue(compare_outputs(output([], [0]), output([], [0, 0])))
        self.assertTrue(compare_outputs(output([]), [{"name": "Value", "branches": []}]))

    def test_null_is_not_missing_or_text(self):
        null = {"kind": "null", "value": None}
        self.assertTrue(compare_outputs(output([null]), output([])))
        self.assertTrue(compare_outputs(output([null]), output([{"kind": "text", "value": ""}])))

    def test_numeric_storage_type_requires_explicit_port_decision(self):
        self.assertTrue(compare_outputs(output([{"kind": "integer", "value": "5"}]), output([number(5)])))

    def test_integer_values_are_exact_above_double_precision(self):
        self.assertTrue(compare_outputs(output([{"kind": "integer", "value": "9007199254740992"}]),
                                        output([{"kind": "integer", "value": "9007199254740993"}])))

    def test_only_floating_values_use_tolerance(self):
        self.assertFalse(compare_outputs(output([number(1)]), output([number(1.0000000005)])))
        self.assertTrue(compare_outputs(output([number(1)]), output([number(1.000000002)])))

    def test_nonfinite_outputs_remain_distinct(self):
        self.assertFalse(compare_outputs(output([number("NaN")]), output([number("NaN")])))
        self.assertTrue(compare_outputs(output([number("Infinity")]), output([number("-Infinity")])))

    def test_unsupported_input_fails_before_any_graph_changes(self):
        for invalid in [True, "2", None, float("nan"), float("inf"), 1e10, 0.1234567890123]:
            with self.subTest(value=invalid), self.assertRaises(ValueError):
                numeric_inputs({"inputs": [invalid]})


if __name__ == "__main__":
    unittest.main()
