def concatenate_session_lists(practice_list, word_list, words_per_list, num_lists):
    """Takes a practice list and a list of all the words for the session.
    Combines them appropriately and adds list numbers.  Does not shuffle.
    Shuffle beforehand please.

    :param practice_list: list of words for practice session.  words are dictionaries {"word":"dog"}
    :param word_list: list of words for rest of session
    :param int words_per_list: Number of words in each list
    :param int num_lists: Total number of lists excluding the practice list.
    :returns: list of dictionaries similar to ``practice_list`` with added ``listno``

    """
    assert len(word_list) == words_per_list * num_lists
    practice_list = assign_list_numbers_from_word_list(practice_list, 1)
    word_list = assign_list_numbers_from_word_list(word_list, num_lists, start=1)
    return practice_list + word_list


def assign_list_numbers_from_word_list(all_words, number_of_lists, start=0):
    """takes a list of dictionaries with just words and adds listnos.

    :param all_words: a list of dictionaries of all the words to assign numbers to
    :param number_of_lists: how many lists should the words be divided into
    :returns a list of dictionaries similar to ``all_words`` with added ``listno``

    """
    if len(all_words) == 0 or number_of_lists == 0:
        return []
    if len(all_words) % number_of_lists != 0:
        raise ValueError("The number of words must be evenly divisible by the number of lists.")

    length_of_each_list = len(all_words)//number_of_lists
    for i in range(len(all_words)):
        all_words[i]['listno'] = (i//length_of_each_list) + start
    return all_words


def assign_list_types_from_type_list(pool, num_baseline, stim_nonstim, num_ps=0):
    """Assign list types to a pool. The types are:

        * ``PRACTICE``
        * ``BASELINE``
        * ``PS``
        * ``STIM``
        * ``NON-STIM``

        :param list pool: Input word pool.  list of dictionaries with (word, listno) keys
        :param int num_baseline: Number of baseline trials *excluding* the practice
        list.
        :param list stim_nonstim:
            a list of "STIM" or "NON-STIM" strings indicating the order of stim and non-stim interleaved lists.
        :param int num_ps: Number of parameter search trials.
        :returns: pool with assigned types
        :rtype: list

        """

    # Check that the inputs match the number of lists
    last_listno = pool[-1]['listno']
    assert last_listno == num_baseline + len(stim_nonstim) + num_ps, "The number of lists and provided type parameters didn't match"

    for i in range(len(pool)):
        word = pool[i]
        if word['listno'] == 0:
            pool[i]['type'] = "PRACTICE"
            pool[i]['stim_channels'] = None
        elif word['listno'] <= num_baseline:
            pool[i]['type'] = "BASELINE"
            pool[i]['stim_channels'] = None
        elif word['listno'] <= num_baseline + num_ps:
            pool[i]['type'] = "PS"
            pool[i]['stim_channels'] = None
        else:
            stimtype = stim_nonstim[word['listno']-num_ps-num_baseline-1]
            pool[i]['type'] = stimtype
            pool[i]['stim_channels'] = (0,) if stimtype == "STIM" else None

    return pool


def assign_multistim_from_stim_channels_list(pool, stimspec_list):
    """Update stim lists to account for multiple stimulation sites.
    :param list pool: Word pool with assigned stim lists. list items are dictionaries with these keys: word, listno, stim_channels, type)
    :param list stimspec_list: List of stimspec tuples in the order they will appear
    :rtype: list
    """
    return _assign_stim_attribute_from_stim_attribute_list(pool, stimspec_list, 'stim_channels')

def assign_amplitudes_from_amplitude_index_list(pool, amplitude_index_list):
    """Update stim lists to account for varying stimulation amplitudes.
    :param list pool: Word pool with assigned stim lists. list items are dictionaries with these keys: word, listno, stim_channels, type)
    :param list amplitude_index_list: List of amplitude indeces (0, 1, or 2) in the order they will appear
    :rtype: list
    """
    return _assign_stim_attribute_from_stim_attribute_list(pool, amplitude_index_list, 'amplitude_index')
    
def _assign_stim_attribute_from_stim_attribute_list(pool, attribute_list, attribute_name):
    """Update stim lists to account for a new attribute.

    :param list pool: Word pool with assigned stim lists. list items are dictionaries with these keys: word, listno, stim_channels, type)
    :param list attribute_list: List of tuples of the new attribute in the order they will appear
    :param list attribute_name: name of the new attribute
    :rtype: list

    """
    assert len(pool) > 0, "Empty pool"

    stim_words = [word for word in pool if word['type'] == "STIM"]
    unique_listnos = set()
    for word in stim_words:
        unique_listnos.add(word['listno'])

    assert len(unique_listnos) == len(attribute_list), "The number of attributes should be the same as the number of stim lists."

    current_attribute_index = -1
    stim_listno = -1
    for i in range(len(pool)):
        word = pool[i]
        if (word['type'] == "STIM"):
            if (word['listno'] != stim_listno):
                stim_listno = word['listno']
                current_attribute_index += 1
            pool[i][attribute_name] = attribute_list[current_stimspec_attribute]

    return pool

def extract_blocks(pool, listnos, num_blocks):
    """Take out lists based on listnos and separate them into blocks

    :param list pool: Input word pool.
    :param list listnos: The order of lists to separate into blocks
    :param int num_blocks: The number of blocks to organize the listnos into
    :returns: blocks of words as a list of tuples

    """
    assert len(listnos) % num_blocks == 0, "The number of lists to append must be divisable by the number of blocks"

    wordlists = {}
    for word in pool:
        wordlists[word['listno']] = wordlists.get(word['listno'], []) + [word]

    blocks = []
    for i in range(len(listnos)):
        listno = listnos[i]
        wordlist = [word.copy() for word in wordlists[listno]]
        for word in wordlist:
            word['blockno'] = i//num_blocks
            word['block_listno'] = i
        blocks += wordlist

    return blocks
