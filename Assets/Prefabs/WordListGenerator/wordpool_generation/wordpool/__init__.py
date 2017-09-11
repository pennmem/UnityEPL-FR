import numpy as np
import pandas as pd
import init_no_pandas
from pkg_resources import resource_filename, resource_listdir

__version__ = "0.4.1"


def list_available_pools():
    """Returns a list of the pools available in the `wordpool.data` package."""
    files = resource_listdir("wordpool", "data")
    return [f for f in files if f.endswith(".txt")]


def load(filename, from_data_package=True):
    """Return contents of a word list.

    :param str filename:
    :param bool from_data_package: When True (the default), load data from the
        ``wordpool.data`` package. Otherwise, treat the filename as an absolute
        path to load arbitrary wordpools from.
    :rtype: pd.DataFrame

    """
    if from_data_package:
        src = resource_filename("wordpool.data", filename)
    else:
        src = filename
    return pd.read_table(src)

def assign_list_numbers(df, n_lists, start=0):
    """Assign or reassign list numbers to all words in the pool.

    :param pd.DataFrame df: Input word pool
    :param int n_lists: Total number of lists.
    :param int start: Start number for lists.
    :returns: Word pool with list numbers assigned

    """
    assert len(df) % n_lists == 0
    words_per_list = int(len(df) / n_lists)
    listnos = np.array(
        [[n]*words_per_list for n in range(start, start + n_lists)], dtype=int).flatten()
    df["listno"] = listnos
    return df


def shuffle_words(df):
    """Shuffle words.

    :param pd.DataFrame df: Input word pool
    :returns: Shuffled pool

    """
    shuffled = df.reindex(np.random.permutation(df.index))
    return shuffled.reset_index(drop=True)


def shuffle_within_groups(df, column):
    """Shuffle within groups of words based on some common values in a column.

    :param pd.DataFrame df: Input word pool
    :param str column: Column name.
    :returns: Pool with groups shuffled.

    """
    if column not in df.columns:
        raise RuntimeError("Column {} not found in DataFrame".format(column))

    shuffled = []
    for col in df[column].unique():
        list_ = df[df[column] == col]
        shuffled.append(list_.reindex(np.random.permutation(list_.index)))

    return pd.concat(shuffled).reset_index(drop=True)


def shuffle_within_lists(df):
    """Shuffle within lists in the pool (i.e., shuffle each list but do not
    move any words between lists. This requires that list
    numbers have alreay been assigned.

    :param pd.DataFrame df: Input word pool
    :returns: Pool with lists shuffled

    """
    if "listno" not in df.columns:
        raise RuntimeError("You must assign list numbers first.")

    return shuffle_within_groups(df, "listno")
