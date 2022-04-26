#!/usr/bin/env python3

from typing import Dict, List, Tuple


header = True
headers = []

to_normalize = set([
    "Ama",
    "Odia",
    "Parla la lingua",
    "Le interessa", 
    "Pratica come sport", 
    "Le piace la musica", 
    "Le piace il genere cinematografico", 
    "Le piace il genere letterario", 
    "Le piace la cucina", 
    "Preferisce come vacanza",
    "Non le piace come vacanza",
    "Ha viaggio ideale"])

special = {}

aux_tables: Dict[str, List[List[str]]] = {}

with open("../../Dati/liberoD.csv") as fp:
    for line in fp:
        fields = line.rstrip().split(';')
        if header:
            headers = fields
            for (i, h) in enumerate(headers):
                if h in to_normalize:
                    special[i] = h
                else:
                    print(h, end='\t')
                # end if
            # end for
            print()
        else:
            for (i, field) in enumerate(fields):
                if i in special:
                    aux_tables.setdefault(special[i], [])
                    if field != '':
                        aux_tables[special[i]].append([fields[0], field])
                    # end if
                else:
                    print(field, end='\t')
                # end if
            # end for
            print()
        # end if
        header = False
    # end for
# end with

for key in aux_tables:
    print(key)
    for value in aux_tables[key]:
        print('\t'.join(value))
    # end for
    print()
# end for