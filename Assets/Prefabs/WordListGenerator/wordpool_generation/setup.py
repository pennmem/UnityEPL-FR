from setuptools import setup
from wordpool import __version__

setup(
    name="wordpool",
    version=__version__,
    description="Word pool generation and tools for memory experiments",
    author="Michael V. DePalatis",
    author_email="depalati@sas.upenn.edu",
    packages=["wordpool"],
    package_data={
        "": ["*.txt", "*.json"]
    },
    install_requires=[
        "numpy",
        "pandas"
    ],
    setup_requires=[
        "pytest-runner"
    ],
    tests_require=[
        "pytest",
        "pytest-cov"
    ]
)
