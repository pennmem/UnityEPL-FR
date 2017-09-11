wordpool
========

.. image:: https://travis-ci.org/pennmem/wordpool.svg?branch=master
    :target: https://travis-ci.org/pennmem/wordpool

Word pool generation and utilities for verbal memory experiments.


Included word pools
-------------------

``wordpool`` ships with some word pools that can be accessed as follows::

  import wordpool

  words = wordpool.load("ram_categorized_en.txt")

Included word pools can be shown with::

  print(wordpool.list_available_pools())
