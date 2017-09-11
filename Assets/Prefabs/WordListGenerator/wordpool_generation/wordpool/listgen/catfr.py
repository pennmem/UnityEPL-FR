"""CatFR list generation utilities."""

import functools
import numpy as np
import pandas as pd

from .. import exc
from .. import load, shuffle_within_groups, shuffle_words


def assign_word_numbers(pool):
    """Assign a serial number to each word in a category. Also assigns category
    numbers (alphabetical sequence).

    """
    pool["wordno"] = -1

    word_count = pool.groupby("category").count().word
    n_words = word_count[0]
    assert all([n_words == word_count[n] for n in range(1, len(word_count))])

    # Assign word and category numbers
    pool["category_num"] = -999
    for n, cat in enumerate(pool.category.unique()):
        pool.loc[pool.category == cat, "wordno"] = range(n_words)
        pool.loc[pool.category == cat, "category_num"] = n

    return pool


def assign_list_numbers(pool, n_lists=26, list_start=1):
    """Assign list numbers to words in the pool."""
    assert "wordno" in pool.columns
    pool["listno"] = -1

    def condition(half, category):
        if half == 0:
            half_cond = pool.wordno % 2 == 0
        else:
            half_cond = pool.wordno % 2 == 1
        return (
            (pool.category == category) &
            (half_cond) &
            (pool.listno == -1)
        )

    original = pool.copy()
    while True:
        pool = original.copy()
        try:
            for listno in range(list_start, n_lists):
                choices = pool.category[(pool.listno == -1)].unique()
                cats = np.random.choice(choices, 3, replace=False)
                ixf, ixs = [], []

                for cat in cats:
                    cond = functools.partial(condition, category=cat)
                    ixf.append(pool.word[cond(0)].sample(2).index)
                    ixs.append(pool.word[cond(1)].sample(2).index)
                ix = pd.Index(np.array(ixf + ixs).flatten())
                pool.loc[ix, "listno"] = listno
            return pool
        except ValueError:
            pass


def sort_pairs(pool):
    """Arrange categorical pairs of words."""
    assert "category" in pool.columns
    assert "listno" in pool.columns
    assert "wordno" in pool.columns

    def cond(category, listno, excluded):
        return (
            (pool.listno == listno) &
            (pool.category == category) &
            ~(pool.word.isin(excluded))
        )

    lists = []
    used = []
    for listno in sorted(pool.listno.unique()):
        order_1 = pool.category[pool.listno == listno].unique()
        order_2 = np.random.permutation(order_1)
        while order_2[0] == order_1[-1]:
            order_2 = np.random.permutation(order_1)
        order = np.append(order_1, order_2)

        list_ = None
        for cat in order:
            row = pool[cond(cat, listno, used)].sample(2)
            list_ = pd.concat([list_, row])
            used.extend(list_.iloc[-2:].word)
        lists.append(list_)

    return pd.concat(lists).reset_index(drop=True)


def generate_session_pool(language="EN"):
    """Generate a single session pool for catFR experiments.

    :param str language: Language to load words in.
    :param int num_lists: Number of lists to assign.
    :param int listno_start: Where to start numbering from.
    :returns: Shuffled, categorized word pool.

    """
    # validate language
    if language.lower() not in ["en", "sp"]:
        raise exc.LanguageError(
            "Language must be 'EN' or 'SP'".format(language))

    # Load and shuffle order of words in categories
    filename = "ram_categorized_{:s}.txt".format(language.lower())
    pool = shuffle_within_groups(load(filename), "category")
    words = sort_pairs(assign_list_numbers(assign_word_numbers(pool)))

    all_words = load('practice_cat_{:s}.txt'.format(language.lower()))
    all_words['listno'] = 0
    all_words['type'] = "PRACTICE"
    all_words['wordno'] = range(12)
    all_words['category'] = "X"
    all_words['category_num'] = -999
    all_words = shuffle_words(all_words)

    return all_words.append(words).reset_index(drop=True)
